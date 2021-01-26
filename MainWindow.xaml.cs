using Microsoft.Win32;
using SoulsFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RootmotionTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public AnimXml Data { get; set; }

        public FrameDataModel FrameData { get; set; } = null;

        public FrameDataModel TempFrameData { get; set; } = null;

        public BND4 anibnd { get; set; } = new BND4();

        public Dictionary<string, Dictionary<string, int>> dropdownOptions { get; set; } // <taeid, <filepath, fileid>>

        public RootmotionToolTempFile OpenFile { get; set; } = new RootmotionToolTempFile();

        public MainWindow()
        {
            InitializeComponent();
            dropdownOptions = new Dictionary<string, Dictionary<string, int>>();
            cmb_moveset.ItemsSource = Enumerable.Empty<string>();
            cmb_animation.ItemsSource = Enumerable.Empty<string>();
        }

        private void HandleTextboxFramedataChange(object sender, RoutedEventArgs e)
        {
            FrameInputBox srcInput = e.Source as FrameInputBox;
            
            bool isParsed = decimal.TryParse(srcInput.Text, out decimal parsedValue);
            if (isParsed)
            {
                if (parsedValue.Equals(srcInput.value)) return;

                srcInput.value = parsedValue;
                TempFrameData.UpdateFrameAxis(srcInput.Frame, srcInput.axis, srcInput.value, isRelative: true);

                DrawGraphs(TempFrameData.Absolute);
            }
            else
            {
                srcInput.Text = srcInput.value.ToString();
            }          
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Canvas.Width = (e.NewSize.Width - 530) *2;
            Canvas.Height = (e.NewSize.Height - 49) * 1000/551;

            if (TempFrameData == null) return;
            DrawGraphs(TempFrameData.Absolute);
        }

        public void DrawGraphs(List<Dictionary<string, decimal>> frameData)
        {
            DrawGraphParams graphX = new DrawGraphParams
            {
                canvas = Canvas,
                linecolor = Brushes.Red,
                graphOrigin = new Point(0, -33),
                graphLength = (int)Canvas.Width - 40,
                graphHeight = ((int)Canvas.Height - 100) /3,
                axis = "X",
                graphData = frameData.Select(f => new Dictionary<string, decimal> {
                        { "X", f["X"] },
                        { "Y", f["Y"] },
                        { "Z", f["Z"] * -1 },
                        { "Rotation", f["Rotation"] }
                    }).ToList()
            };

            DrawGraphParams graphY = new DrawGraphParams(graphX)
            {
                axis = "Y",
                linecolor = Brushes.Yellow,
                graphOrigin = new Point(0, -66 - (((int)Canvas.Height - 100) /3))
            };

            DrawGraphParams graphZ = new DrawGraphParams(graphX)
            {
                axis = "Z",
                linecolor = Brushes.Blue,
                graphOrigin = new Point(0, -100 - 2 * (((int)Canvas.Height - 100) / 3))
            };

            Canvas.Children.Clear();
            new DrawGraph(graphX);
            new DrawGraph(graphY);
            new DrawGraph(graphZ);
        }

        public void OpenFrameData(string xmlFile)
        {
            Data = new AnimXml(xmlFile);
            var (rootmotionData, rootmotionNotFound) = Data.GetRootmotionData();

            if (rootmotionNotFound)
            {
                TextBlock textBlock = new TextBlock
                {
                    Text = "No Rootmotion Found to Edit",
                    Foreground = Brushes.Red,
                    FontSize = 60,
                };

                Canvas.Children.Add(textBlock);
                OpenFile.xmlPath = string.Empty;
                OpenFile.hkxPath = string.Empty;
                return;
            }

            var (frameData, parsingError) = Data.processRootmotionData(rootmotionData);
            if (!parsingError)
            {
                FrameData = new FrameDataModel(frameData);
                TempFrameData = new FrameDataModel(frameData);

                StackPanel.Children.Clear();
                foreach (var frame in FrameData.Relative.Select((motion, index) => (motion, index)))
                {
                    StackPanel newPanel = new StackPanel
                    {
                        Height = 40,
                        Orientation = Orientation.Horizontal
                    };

                    Label Frame = new Label { Content = frame.index, Background = Brushes.LightGray, Width = 60, VerticalContentAlignment = VerticalAlignment.Center };
                    FrameInputBox X = new FrameInputBox { Text = frame.motion["X"].ToString(), Background = Brushes.IndianRed, Width = 150, VerticalContentAlignment = VerticalAlignment.Center, BorderThickness = new Thickness(0) };
                    FrameInputBox Y = new FrameInputBox { Text = frame.motion["Y"].ToString(), Background = Brushes.LightYellow, Width = 150, VerticalContentAlignment = VerticalAlignment.Center, BorderThickness = new Thickness(0) };
                    FrameInputBox Z = new FrameInputBox { Text = frame.motion["Z"].ToString(), Background = Brushes.DodgerBlue, Width = 150, VerticalContentAlignment = VerticalAlignment.Center, BorderThickness = new Thickness(0) };

                    X.Frame = frame.index;
                    Y.Frame = frame.index;
                    Z.Frame = frame.index;

                    X.axis = "X";
                    Y.axis = "Y";
                    Z.axis = "Z";

                    X.value = frame.motion["X"];
                    Y.value = frame.motion["Y"];
                    Z.value = frame.motion["Z"];

                    X.LostFocus += HandleTextboxFramedataChange;
                    Y.LostFocus += HandleTextboxFramedataChange;
                    Z.LostFocus += HandleTextboxFramedataChange;

                    newPanel.Children.Add(Frame);
                    newPanel.Children.Add(X);
                    newPanel.Children.Add(Y);
                    newPanel.Children.Add(Z);

                    StackPanel.Children.Add(newPanel);
                }

                DrawGraphs(frameData);
            }
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(OpenFile.xmlPath)) return;
            if (string.IsNullOrEmpty(OpenFile.bndPath)) return;

            Data.UpdateRootmotionData(AnimXml.FrameDataToString(TempFrameData.Absolute));
            Data.Save(OpenFile.xmlPath);

            string hkxpacksoulsPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "hkxpackds3.exe");
            HKXPack packer = new HKXPack(hkxpacksoulsPath);
            OpenFile.hkxPath = packer.Pack(OpenFile.xmlPath);

            OpenFile.extractedFile.File.Bytes = File.ReadAllBytes(OpenFile.hkxPath);

            if (!anibnd.Files.Any(f => f.ID == OpenFile.extractedFile.File.ID)) return;
            int listID = anibnd.Files.FindIndex(f => f.ID == OpenFile.extractedFile.File.ID);
            anibnd.Files[listID] = OpenFile.extractedFile.File;


            FileInfo bakFileInfo = new FileInfo(OpenFile.bndPath + ".bak");
            FileInfo bndFileInfo = new FileInfo(OpenFile.bndPath);
            bakFileInfo.IsReadOnly = false;
            bndFileInfo.IsReadOnly = false;

            File.Delete(OpenFile.bndPath + ".bak");
            File.Move(OpenFile.bndPath, OpenFile.bndPath + ".bak");
            anibnd.Write(OpenFile.bndPath);
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.DefaultExt = ".anibnd.dcx";
            fileDialog.Filter = "ANIBND.DCX Files (*.anibnd.dcx)|*.anibnd.dcx";
            fileDialog.Title = "Select a .animbnd.dcx file";
            fileDialog.CheckFileExists = true;
            fileDialog.CheckPathExists = true;
            bool? result = fileDialog.ShowDialog();

            if (result == true)
            {
                StackPanel.Children.Clear();
                Canvas.Children.Clear();

                string filePath = fileDialog.FileName;
                Console.WriteLine(filePath);

                if (!File.Exists(filePath)) return;
                if (!BND4.Is(DCX.Decompress(filePath))) return;

                anibnd = BND4.Read(filePath);
                OpenFile.bndPath = filePath;
                dropdownOptions = new Dictionary<string, Dictionary<string, int>>();
                foreach (BND4.File f in anibnd.Files)
                {
                    if (System.IO.Path.GetExtension(f.Name).Equals(".hkx"))
                    {
                        Console.WriteLine(f.ToString());
                        string moveset = new DirectoryInfo(f.Name).Parent.Name;
                        string fileName = System.IO.Path.GetFileNameWithoutExtension(f.Name);

                        if (!dropdownOptions.ContainsKey(moveset)) dropdownOptions[moveset] = new Dictionary<string, int>();
                        dropdownOptions[moveset][fileName] = f.ID;
                    }
                }

                lbl_anibnd.Content = System.IO.Path.GetFileNameWithoutExtension(filePath);
                cmb_moveset.ItemsSource = dropdownOptions.Keys.Count > 0 ? dropdownOptions.Keys : Enumerable.Empty<string>();
                cmb_animation.ItemsSource = Enumerable.Empty<string>();
            }
        }

        private void cmb_moveset_DropDownClosed(object sender, EventArgs e)
        {
            cmb_animation.ItemsSource = dropdownOptions.ContainsKey(cmb_moveset.Text) ? dropdownOptions[cmb_moveset.Text].Keys : Enumerable.Empty<string>();
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            StackPanel.Children.Clear();
            Canvas.Children.Clear();
            if (anibnd.Files.Count < 1) return;
            if (!cmb_moveset.HasItems) return;
            if (!cmb_animation.HasItems) return;
            if (cmb_moveset.SelectedItem == null) return;
            if (cmb_animation.SelectedItem == null) return;
            if (!dropdownOptions.ContainsKey(cmb_moveset.Text)) return;
            if (!dropdownOptions[cmb_moveset.Text].ContainsKey(cmb_animation.Text)) return;

            int fileID = dropdownOptions[cmb_moveset.Text][cmb_animation.Text];
            if (!anibnd.Files.Any(f => f.ID == fileID)) return;

            string extractPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RootmotionToolTemp.hkx");
            string hkxpacksoulsPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "hkxpackds3.exe");

            var extractedFile = BND4Extract.Extract(anibnd, fileID, extractPath);

            HKXPack unpacker = new HKXPack(hkxpacksoulsPath);
            string unpackedFilePath = unpacker.Unpack(extractedFile.Path);

            OpenFile.extractedFile = extractedFile;
            OpenFile.hkxPath = extractedFile.Path;
            OpenFile.xmlPath = unpackedFilePath;

            OpenFrameData(OpenFile.xmlPath);
        }
    }

    public class RootmotionToolTempFile
    {
        public RootmotionToolTempFile()
        {
        }

        public string bndPath { get; set; }

        public string hkxPath { get; set; }

        public string xmlPath { get; set; }

        public BND4Extract.ExtractedFile extractedFile { get; set; }
    }

    class FrameInputBox : TextBox
    {
        public int Frame { get; set; }

        public string axis { get; set; }

        public decimal value { get; set; }
    }

    class Point
    {
        public decimal x { get; set; }

        public decimal y { get; set; }

        public Point(decimal extx, decimal exty)
        {
            x = extx;
            y = exty;
        }
    }

    class DrawGraphParams
    {
        public Canvas canvas { get; set; }
        public Point graphOrigin { get; set; }
        public int graphLength { get; set; }
        public int graphHeight { get; set; }
        public string axis { get; set; }
        public List<Dictionary<string, decimal>> graphData { get; set; }
        public Brush linecolor { get; set; }

        public DrawGraphParams()
        {
        }

        public DrawGraphParams(DrawGraphParams cloneSource)
        {
            canvas = cloneSource.canvas;
            graphOrigin = cloneSource.graphOrigin;
            graphLength = cloneSource.graphLength;
            graphHeight = cloneSource.graphHeight;
            axis = cloneSource.axis;
            graphData = cloneSource.graphData;
            linecolor = cloneSource.linecolor;
        }
    }

    class DrawGridlineParams
    {
        public enum GridlineType{
            Main,
            sub
        }
        public decimal X1 { get; set; }
        public decimal X2 { get; set; }
        public decimal Y1 { get; set; }
        public decimal Y2 { get; set; }
        public GridlineType gridlineType { get; set; }
        public Canvas canvas { get; set; }
        public int graphHeight { get; set; }
        public int graphLength { get; set; }
        public decimal margin { get; set; }
    }

    class DrawTextParams
    {
        public decimal X { get; set; }
        public decimal Y { get; set; }
        public string text { get; set; }
        public Brush color { get; set; }
        public Canvas canvas { get; set; }
        public int graphHeight { get; set; }
        public int graphLength { get; set; }
    }

    class DrawGraph
    {
        private decimal findMax(string axis, List<Dictionary<string, decimal>> graphData)
        {
            return graphData.Select(p => p[axis]).Max();
        }

        private decimal findMin(string axis, List<Dictionary<string, decimal>> graphData)
        {
            return graphData.Select(p => p[axis]).Min();
        }

        private void drawGridline(DrawGridlineParams gridlineParams)
        {
            Line gridline = new Line
            {
                Stroke = Brushes.Black,
                StrokeThickness = 1,
                X1 = decimal.ToDouble(gridlineParams.X1),
                X2 = decimal.ToDouble(gridlineParams.X2),
                Y1 = decimal.ToDouble(gridlineParams.Y1) * -1 + gridlineParams.graphHeight,
                Y2 = decimal.ToDouble(gridlineParams.Y2) * -1 + gridlineParams.graphHeight,
            };

            if (DrawGridlineParams.GridlineType.Main.Equals(gridlineParams.gridlineType))
            {
                gridline.StrokeThickness = 4;

                Line gridlineExtension = new Line
                {
                    Stroke = Brushes.Black,
                    StrokeThickness = 1,
                    X1 = decimal.ToDouble(gridlineParams.X2),
                    X2 = gridlineParams.X1 == gridlineParams.X2
                        ? decimal.ToDouble(gridlineParams.X2)
                        : decimal.ToDouble(gridlineParams.X1) + gridlineParams.graphLength - decimal.ToDouble(gridlineParams.margin)/2,
                    Y1 = decimal.ToDouble(gridlineParams.Y2) * -1 + gridlineParams.graphHeight,
                    Y2 = gridlineParams.Y1 == gridlineParams.Y2 
                        ? decimal.ToDouble(gridlineParams.Y2) * -1 + gridlineParams.graphHeight
                        : (decimal.ToDouble(gridlineParams.Y1) + gridlineParams.graphHeight - decimal.ToDouble(gridlineParams.margin)/2) * -1 + gridlineParams.graphHeight
                };

                gridlineParams.canvas.Children.Add(gridlineExtension);
            }

            gridlineParams.canvas.Children.Add(gridline);
        }

        private void drawText(DrawTextParams textParams)
        {
            TextBlock textBlock = new TextBlock
            {
                Text = textParams.text,
                Foreground = textParams.color,
                FontSize = 20,
            };
            Canvas.SetLeft(textBlock, decimal.ToDouble(textParams.X));
            Canvas.SetTop(textBlock, (decimal.ToDouble(textParams.Y) + 14) * -1 + textParams.graphHeight);
            textParams.canvas.Children.Add(textBlock);
        }

        private bool isMultipleOf(decimal value, decimal n)
        {
            return ((decimal)value % (decimal)n) == 0;
        }

        private decimal TruncateToNearest(decimal passedNumber, decimal roundTo)
        {
            if (roundTo == 0)
            {
                return passedNumber;
            }
            else
            {
                return Math.Truncate(passedNumber / roundTo) * roundTo;
            }
        }

        public DrawGraph(DrawGraphParams graphParams)
        {
            decimal margin = 80;
            decimal framecount = graphParams.graphData.Count() - 1;
            decimal horizontalScaler = (graphParams.graphLength - margin) / framecount;

            decimal axisMax = findMax(graphParams.axis, graphParams.graphData);
            decimal axisMin = findMin(graphParams.axis, graphParams.graphData);
            decimal axisSpread = axisMax - axisMin;
            decimal verticalScaler = axisSpread > 0 ? (graphParams.graphHeight - margin) / axisSpread : 1;
            decimal verticalOffset = - axisMin;

            foreach (var point in graphParams.graphData.Select((value, frame) => new { value, frame}).Skip(1))
            {
                var previousPoint = new { value = graphParams.graphData[point.frame - 1], frame = point.frame - 1};

                Line line = new Line
                {
                    Stroke = graphParams.linecolor,
                    StrokeThickness = 4,

                    X1 = previousPoint.frame * decimal.ToDouble(horizontalScaler) + decimal.ToDouble(graphParams.graphOrigin.x) + decimal.ToDouble(margin),
                    X2 = point.frame * decimal.ToDouble(horizontalScaler) + decimal.ToDouble(graphParams.graphOrigin.x) + decimal.ToDouble(margin),

                    Y1 = (decimal.ToDouble(previousPoint.value[graphParams.axis]) + decimal.ToDouble(verticalOffset)) * decimal.ToDouble(verticalScaler) + decimal.ToDouble(graphParams.graphOrigin.y) + decimal.ToDouble(margin),
                    Y2 = (decimal.ToDouble(point.value[graphParams.axis]) + decimal.ToDouble(verticalOffset)) * decimal.ToDouble(verticalScaler) + decimal.ToDouble(graphParams.graphOrigin.y) + decimal.ToDouble(margin)
                };

                // flip Y coordinates to adjust for the inverted Y axis of a canvas.
                line.Y1 = line.Y1 * -1 + graphParams.graphHeight;
                line.Y2 = line.Y2 * -1 + graphParams.graphHeight;

                graphParams.canvas.Children.Add(line);

                drawGridline(new DrawGridlineParams
                {
                    gridlineType = isMultipleOf(point.frame, 10) ? DrawGridlineParams.GridlineType.Main : DrawGridlineParams.GridlineType.sub,
                    canvas = graphParams.canvas,
                    graphHeight = graphParams.graphHeight,
                    graphLength = graphParams.graphLength,
                    margin = margin,
                    X1 = point.frame * horizontalScaler + graphParams.graphOrigin.x + margin,
                    X2 = point.frame * horizontalScaler + graphParams.graphOrigin.x + margin,
                    Y1 = graphParams.graphOrigin.y + margin - 20,
                    Y2 = graphParams.graphOrigin.y + margin - 10
                });

                if (isMultipleOf(point.frame, 10))
                {
                    drawText(new DrawTextParams
                    {
                        canvas = graphParams.canvas,
                        color = Brushes.Black,
                        text = point.frame.ToString(),
                        graphHeight = graphParams.graphHeight,
                        graphLength = graphParams.graphLength,
                        X = point.frame * horizontalScaler + graphParams.graphOrigin.x + margin - 10,
                        Y = graphParams.graphOrigin.y + margin - 30
                    });
                }

                if (previousPoint.frame == 0) drawGridline(new DrawGridlineParams
                {
                    gridlineType = DrawGridlineParams.GridlineType.Main,
                    canvas = graphParams.canvas,
                    graphHeight = graphParams.graphHeight,
                    graphLength = graphParams.graphLength,
                    margin = margin,
                    X1 = previousPoint.frame * horizontalScaler + graphParams.graphOrigin.x + margin,
                    X2 = previousPoint.frame * horizontalScaler + graphParams.graphOrigin.x + margin,
                    Y1 = graphParams.graphOrigin.y + margin - 20,
                    Y2 = graphParams.graphOrigin.y + margin - 10
                });
            }

            (decimal verticalGridlineInterval, decimal verticalMainGridlineInterval) = axisSpread <= 2M
                ? (0.1M, 0.5M)
                : axisSpread <= 5M
                ? (0.25M, 1M)
                : axisSpread <= 10M
                ? (0.5M, 1M)
                : axisSpread <= 20M
                ? (1M, 5M)
                : (1M, 10M);

            for (decimal i = TruncateToNearest(axisMin, verticalGridlineInterval); i - axisMax < verticalGridlineInterval; i += verticalGridlineInterval)
            {
                drawGridline(new DrawGridlineParams
                {
                    gridlineType = isMultipleOf(i, verticalMainGridlineInterval) ? DrawGridlineParams.GridlineType.Main : DrawGridlineParams.GridlineType.sub,
                    canvas = graphParams.canvas,
                    graphHeight = graphParams.graphHeight,
                    graphLength = graphParams.graphLength,
                    margin = margin,
                    X1 = graphParams.graphOrigin.x + margin - 20,
                    X2 = graphParams.graphOrigin.x + margin - 10,
                    Y1 = (i + verticalOffset) * verticalScaler + graphParams.graphOrigin.y + margin,
                    Y2 = (i + verticalOffset) * verticalScaler + graphParams.graphOrigin.y + margin
                });

                if (isMultipleOf(i, verticalMainGridlineInterval))
                {
                    drawText(new DrawTextParams
                    {
                        canvas = graphParams.canvas,
                        color = Brushes.Black,
                        text = graphParams.axis == "Z" ? (-i).ToString() : i.ToString(),
                        graphHeight = graphParams.graphHeight,
                        graphLength = graphParams.graphLength,
                        X = graphParams.graphOrigin.x + 5,
                        Y = (i + verticalOffset) * verticalScaler + graphParams.graphOrigin.y + margin
                    });
                }
            }

            string axisinfo = "";
            switch (graphParams.axis) {
                case "X":
                    axisinfo = "+ left     - right";
                    break;
                case "Y":
                    axisinfo = "+ up     - down";
                    break;
                case "Z":
                    axisinfo = "+ backward     - forward";
                    break;
            }

            drawText(new DrawTextParams
            {
                canvas = graphParams.canvas,
                color = Brushes.Black,
                text = axisinfo,
                graphHeight = graphParams.graphHeight,
                graphLength = graphParams.graphLength,
                X = graphParams.graphOrigin.x + margin,
                Y = graphParams.graphOrigin.y + 25
            });
        }
    }
}
