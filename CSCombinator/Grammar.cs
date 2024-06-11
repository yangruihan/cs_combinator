using System.Collections.Generic;
using System.Text;

namespace CSCombinator
{
    public class Grammar
    {
        private readonly Dictionary<string, Combinator> _products = new Dictionary<string, Combinator>();

        public static string GetWaitToTempSymbolName(string name)
        {
            return $"{Common.InnerSymbol}{{Grammar}}[\"{name}\"]";
        }

        public static string ReplaceTempSymbolName(string s, string grammarName)
        {
            return s.Replace($"{Common.InnerSymbol}{{Grammar}}", grammarName);
        }

        public Combinator this[string key]
        {
            get
            {
                if (_products.TryGetValue(key, out var c))
                {
                    return c;
                }

                c = new Combinator(key, key, GetWaitToTempSymbolName(key), null);
                _products.Add(key, c);
                return c;
            }

            set
            {
                if (!_products.TryGetValue(key, out var c))
                {
                    value.Name = key;
                    _products.Add(key, value);
                }
                else
                {
                    if (value.ParseCb == null)
                    {
                        value.Parent = c;
                        c.Info = value.Info;
                        c.Code = value.Code;
                    }
                    else
                    {
                        c.ParseCb = value.ParseCb;
                        c.Info = value.Info;
                        c.Code = value.Code;

                        var parent = c.Parent;
                        c.Parent = null;

                        while (parent != null)
                        {
                            parent.ParseCb = value.ParseCb;
                            var p = parent.Parent;
                            parent.Parent = null;
                            parent = p;
                        }
                    }
                }
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            foreach (var pair in _products)
            {
                sb.AppendLine($"{pair.Key} = {pair.Value};");
            }

            return sb.ToString();
        }

        public string ToCodeString(string grammarName = "_g")
        {
            var sb = new StringBuilder();

            foreach (var pair in _products)
            {
                if (pair.Key == "EOF")
                {
                    continue;
                }

                sb.AppendLine(
                    $"{grammarName}[\"{pair.Key}\"] = {ReplaceTempSymbolName(pair.Value.Code, grammarName)};");
            }

            sb.AppendLine($"{grammarName}[\"EOF\"] = Eof();");

            return sb.ToString();
        }
    }
}