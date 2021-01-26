using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace RootmotionTool
{
    public class AnimXml
    {
        public XmlDocument XmlData { get; private set; }

        public AnimXml()
        {
            XmlData = new XmlDocument();
        }

        public AnimXml(string filepath)
        {
            XmlData = new XmlDocument();
            XmlData.Load(filepath);
        }

        public void Load(string filepath)
        {
            XmlData.Load(filepath);
        }

        public void Save(string filepath)
        {
            XmlData.Save(filepath);
        }

        public void UpdateRootmotionData(string innerText)
        {
            XmlNode ReferenceFrameSamples = XmlData.SelectSingleNode("//hkparam[@name='referenceFrameSamples']");
            ReferenceFrameSamples.InnerText = innerText;
        }

        public (string, bool) GetRootmotionData()
        {
            XmlNode ReferenceFrameSamples = XmlData.SelectSingleNode("//hkparam[@name='referenceFrameSamples']");
            if (ReferenceFrameSamples == null) return (null, true);
            return (ReferenceFrameSamples.InnerText, false);
        }

        public (List<Dictionary<string, decimal>>, bool) processRootmotionData(string data)
        {
            bool parsingError = false;
            string pattern = @"\( *(?<X>[+-]?\d+\.\d+) +(?<Y>[+-]?\d+\.\d+) +(?<Z>[+-]?\d+\.\d+) +(?<Rotation>[+-]?\d+\.\d+) *\)";
            List<Dictionary<string, decimal>> frameData = new List<Dictionary<string, decimal>>();

            foreach(Match match in Regex.Matches(data, pattern))
            {
                Dictionary<string, decimal> frame = new Dictionary<string, decimal>();
                string[] keys = { "X", "Y", "Z", "Rotation" };

                foreach(string key in keys)
                {
                    bool isParsed = decimal.TryParse(match.Groups[key].Value, out decimal motion);
                    frame.Add(key, isParsed ? motion : 0.0M);
                    if (!isParsed) parsingError = true;
                }

                frameData.Add(frame);
            }
            return (frameData, parsingError);
        }

        public static string FrameDataToString(List<Dictionary<string, decimal>> framedata)
        {
            string accumulator = string.Empty;

            for (int i = 0; i < framedata.Count; i++)
            {
                accumulator += i != 1 ? "\n" : " ";
                accumulator += 
                    "("   + framedata[i]["X"].ToString("0.0################")
                    + " " + framedata[i]["Y"].ToString("0.0################")
                    + " " + framedata[i]["Z"].ToString("0.0################")
                    + " " + framedata[i]["Rotation"].ToString("0.0################")
                    + ")";
            }

            return accumulator;
        }
    }
}
