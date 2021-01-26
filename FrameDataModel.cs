using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Media3D;

namespace RootmotionTool
{
    public class FrameDataModel
    {
        private List<Dictionary<string, decimal>> _framedata;

        public List<Dictionary<string, decimal>> Absolute
        {
            get { return _framedata; }
            set { _framedata = value; }
        }

        public List<Dictionary<string, decimal>> Relative
        {
            get { return ConvertAbsoluteToRelative(_framedata); }
            set { _framedata = ConvertRelativeToAbsolute(value); }
        }

        public FrameDataModel()
        {
        }

        public FrameDataModel(List<Dictionary<string, decimal>> absoluteFramedata)
        {
            Absolute = absoluteFramedata;
        }

        private List<Dictionary<string, decimal>> ConvertAbsoluteToRelative(List<Dictionary<string, decimal>> absoluteFramedata)
        {
            var relativeData = absoluteFramedata.Skip(1).Select((motion, PreviousFrame) => new Dictionary<string, decimal> {
                    { "X", motion["X"] - absoluteFramedata[PreviousFrame]["X"] },
                    { "Y", motion["Y"] - absoluteFramedata[PreviousFrame]["Y"] },
                    { "Z", motion["Z"] - absoluteFramedata[PreviousFrame]["Z"]},
                    { "Rotation", motion["Rotation"] - absoluteFramedata[PreviousFrame]["Rotation"]}
                });

            return (new[] { absoluteFramedata.First() }).Concat(relativeData).ToList();
        }

        private List<Dictionary<string, decimal>> ConvertRelativeToAbsolute(List<Dictionary<string, decimal>> relativeFramedata)
        {
            var accumulator = new Dictionary<string, decimal> 
            {
                { "X", 0 },
                { "Y", 0 },
                { "Z", 0 },
                { "Rotation", 0 }
            };
            
            return relativeFramedata.Select(motion => new Dictionary<string, decimal>
            {
                { "X", accumulator["X"] += motion["X"] },
                { "Y", accumulator["Y"] += motion["Y"] },
                { "Z", accumulator["Z"] += motion["Z"] },
                { "Rotation", accumulator["Rotation"] += motion["Rotation"] }
            }).ToList();
        }

        public void UpdateFrame(int frame, decimal X = decimal.MaxValue, decimal Y = decimal.MaxValue, decimal Z = decimal.MaxValue, decimal Rotation = decimal.MaxValue, bool isRelative = false)
        {
            var framedata = isRelative ? Relative : Absolute;

            if (framedata.ElementAtOrDefault(frame) == null) return;

            var newFrame = new Dictionary<string, decimal> 
            {
                { "X", decimal.MaxValue.Equals(X) ? framedata[frame]["X"] : X },
                { "Y", decimal.MaxValue.Equals(Y) ? framedata[frame]["Y"] : Y },
                { "Z", decimal.MaxValue.Equals(Z) ? framedata[frame]["Z"] : Z },
                { "Rotation", decimal.MaxValue.Equals(Rotation) ? framedata[frame]["Rotation"] : Rotation },
            };

            framedata[frame] = newFrame;

            if (isRelative)
            {
                Relative = framedata;
                Console.WriteLine(Relative[frame]["X"]);
            }
            else
            {
                Absolute = framedata;
            }
        }

        public void UpdateFrameAxis(int frame, string axis, decimal value, bool isRelative = false)
        {
            switch (axis)
            {
                case "X":
                    UpdateFrame(frame, X: value, isRelative: isRelative);
                    break;
                case "Y":
                    UpdateFrame(frame, Y: value, isRelative: isRelative);
                    break;
                case "Z":
                    UpdateFrame(frame, Z: value, isRelative: isRelative);
                    break;
                case "Rotation":
                    UpdateFrame(frame, Rotation: value, isRelative: isRelative);
                    break;
            }
        }
    }
}
