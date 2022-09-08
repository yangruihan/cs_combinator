using System.Collections.Generic;
using System.Text;

namespace CSConbinator
{
    public class Grammar
    {
        private readonly Dictionary<string, Combinator> _products = new Dictionary<string, Combinator>();

        public Combinator this[string key]
        {
            get
            {
                if (_products.TryGetValue(key, out var c))
                {
                    return c;
                }

                c = new Combinator(key, key, null);
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
                    }
                    else
                    {
                        c.ParseCb = value.ParseCb;
                        c.Info = value.Info;

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
    }
}