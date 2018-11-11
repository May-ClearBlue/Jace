using System.Collections.Generic;
using System.Globalization;

namespace Jace.Operations
{
    public class VariableCalcurator : Operation
    {
        public string paramString;
        public bool lastResult = false;

        public VariableCalcurator(DataType dataType, string param)
            : base(dataType, false)
        {
            paramString = param;
//            this.DataType = type;
        }

        public VariableCalcurator(bool param) : base(DataType.Boolean , false) { paramString = param.ToString(); }
        public VariableCalcurator(int param) : base(DataType.Integer, false) { paramString = param.ToString(); }
        public VariableCalcurator(float param) : base(DataType.FloatingPoint, false){ paramString = param.ToString(); }
        public VariableCalcurator(string param) : base(DataType.Literal, false) { paramString = param; }
        public VariableCalcurator(uint param) : base(DataType.UnsighnedInteger, false) { paramString = param.ToString(); }

        public bool Bool(bool defaultValue = false)
        {
            bool result = false;
            return (lastResult = bool.TryParse(paramString, out result)) ? result : defaultValue;
        }
        public float Float(float defaultValue = 0.0f)
        {
            float result = 0.0f;
            return (lastResult = float.TryParse(paramString, out result)) ? result : defaultValue;
        }

        public int Int(int defaultValue = 0)
        {
            int result = 0;
            return (lastResult = int.TryParse(paramString, out result)) ? result : defaultValue;
        }

        public uint Uint(uint defaultValue = 0)
        {
            uint result = 0;
            return (lastResult = uint.TryParse(paramString, out result)) ? result : defaultValue;
        }

        public string Literal(string defaultValue = null)
        {
            return paramString;
        }

        public static VariableCalcurator operator +(VariableCalcurator src, VariableCalcurator dest)
        {
            switch (src.DataType)
            {
                case DataType.Boolean:
                    return new VariableCalcurator(true);
                case DataType.Integer:
                    return new VariableCalcurator(src.Int() + dest.Int());
                case DataType.FloatingPoint:
                    return new VariableCalcurator(src.Float() + dest.Float());
                case DataType.UnsighnedInteger:
                    return new VariableCalcurator(src.Uint() + dest.Uint());
                case DataType.Literal:
                    return new VariableCalcurator(src.Literal() + dest.Literal());
                default:
                    throw new VariableNotDefinedException(string.Format("this '+'is not defined."));
            }
        }

        public static VariableCalcurator operator -(VariableCalcurator src, VariableCalcurator dest)
        {
            switch (src.DataType)
            {
                case DataType.Boolean:
                    return new VariableCalcurator(false);
                case DataType.Integer:
                    return new VariableCalcurator(src.Int() - dest.Int());
                case DataType.FloatingPoint:
                    return new VariableCalcurator(src.Float() - dest.Float());
                case DataType.UnsighnedInteger:
                    return new VariableCalcurator(src.Uint() - dest.Uint());
                case DataType.Literal:
                    return new VariableCalcurator(string.Empty);
                default:
                    throw new VariableNotDefinedException(string.Format("this '-'is not defined."));
            }
        }

        public static VariableCalcurator operator -(VariableCalcurator src)
        {
            switch (src.DataType)
            {
                case DataType.Boolean:
                    return new VariableCalcurator(false);
                case DataType.Integer:
                    return new VariableCalcurator(src.Int() * -1);
                case DataType.FloatingPoint:
                    return new VariableCalcurator(src.Float() * -1.0f);
//                case DataType.UnsighnedInteger:
//                    return new VariableCalcurator(src.Uint() * -1);
 //               case DataType.Literal:
 //                   return new VariableCalcurator(string.Empty);
                default:
                    throw new VariableNotDefinedException(string.Format("this '-'is not defined."));
            }
        }

        public static VariableCalcurator operator *(VariableCalcurator src, VariableCalcurator dest)
        {
            switch (src.DataType)
            {
                //                case TRDataType.Bool:
                //                    return new VariableCalcurator(true);
                case DataType.Integer:
                    return new VariableCalcurator(src.Int() * dest.Int());
                case DataType.FloatingPoint:
                    return new VariableCalcurator(src.Float() * dest.Float());
                case DataType.UnsighnedInteger:
                    return new VariableCalcurator(src.Uint() * dest.Uint());
                //                case TRDataType.Literal:
                //                    return new VariableCalcurator(string.Empty);
                default:
                    throw new VariableNotDefinedException(string.Format("this '*'is not defined."));
            }
        }

        public static VariableCalcurator operator /(VariableCalcurator src, VariableCalcurator dest)
        {
            switch (src.DataType)
            {
                //               case TRDataType.Bool:
                //                    return new VariableCalcurator(true);
                case DataType.Integer:
                    return new VariableCalcurator(src.Int() / dest.Int());
                case DataType.FloatingPoint:
                    return new VariableCalcurator(src.Float() / dest.Float());
                case DataType.UnsighnedInteger:
                    return new VariableCalcurator(src.Uint() / dest.Uint());
                //                case TRDataType.Literal:
                //                    return new VariableCalcurator(src.Literal() + dest.Literal());
                default:
                    throw new VariableNotDefinedException(string.Format("this '/'is not defined."));
            }
        }

        public static VariableCalcurator operator %(VariableCalcurator src, VariableCalcurator dest)
        {
            switch (src.DataType)
            {
//                case DataType.Boolean:
//                    return new VariableCalcurator(true);
                case DataType.Integer:
                    return new VariableCalcurator(src.Int() % dest.Int());
                case DataType.FloatingPoint:
                    return new VariableCalcurator(src.Float() % dest.Float());
                case DataType.UnsighnedInteger:
                    return new VariableCalcurator(src.Uint() % dest.Uint());
//                case DataType.Literal:
//                    return new VariableCalcurator(src.Literal() + dest.Literal());
                default:
                    throw new VariableNotDefinedException(string.Format("this '%'is not defined."));
            }
        }

        public static VariableCalcurator operator &(VariableCalcurator src, VariableCalcurator dest)
        {
            switch (src.DataType)
            {
                case DataType.Boolean:
                    return new VariableCalcurator(src.Bool() && dest.Bool());
                case DataType.Integer:
                    return new VariableCalcurator(src.Int() != 0 && dest.Int() != 1);
                case DataType.FloatingPoint:
                    return new VariableCalcurator(src.Float() != 0.0f && dest.Float() != 0.0f);
                case DataType.UnsighnedInteger:
                    return new VariableCalcurator(src.Uint() != 0 && dest.Uint() != 0);
                case DataType.Literal:
                    return new VariableCalcurator(string.IsNullOrEmpty(src.Literal()) && string.IsNullOrEmpty(dest.Literal()));
                default:
                    throw new VariableNotDefinedException(string.Format("this '&&'is not defined."));
            }
        }

        public static VariableCalcurator operator |(VariableCalcurator src, VariableCalcurator dest)
        {
            switch (src.DataType)
            {
                case DataType.Boolean:
                    return new VariableCalcurator(src.Bool() || dest.Bool());
                case DataType.Integer:
                    return new VariableCalcurator((src.Int() != 0) || (dest.Int() != 1));
                case DataType.FloatingPoint:
                    return new VariableCalcurator((src.Float() != 0.0f) || (dest.Float() != 0.0f));
                case DataType.UnsighnedInteger:
                    return new VariableCalcurator((src.Uint() != 0) || (dest.Uint() != 0));
                case DataType.Literal:
                    return new VariableCalcurator(string.IsNullOrEmpty(src.Literal()) || string.IsNullOrEmpty(dest.Literal()));
                default:
                    throw new VariableNotDefinedException(string.Format("this '||'is not defined."));
            }
        }

        public static VariableCalcurator operator ==(VariableCalcurator src, VariableCalcurator dest)
        {
            switch (src.DataType)
            {
                case DataType.Boolean:
                    return new VariableCalcurator(src.Bool() == dest.Bool());
                case DataType.Integer:
                    return new VariableCalcurator((src.Int() != 0) == (dest.Int() != 1));
                case DataType.FloatingPoint:
                    return new VariableCalcurator((src.Float() != 0.0f) == (dest.Float() != 0.0f));
                case DataType.UnsighnedInteger:
                    return new VariableCalcurator((src.Uint() != 0) == (dest.Uint() != 0));
                case DataType.Literal:
                    return new VariableCalcurator(string.IsNullOrEmpty(src.Literal()) == string.IsNullOrEmpty(dest.Literal()));
                default:
                    throw new VariableNotDefinedException(string.Format("this '||'is not defined."));
            }
        }

        public static VariableCalcurator operator !=(VariableCalcurator src, VariableCalcurator dest)
        {
            switch (src.DataType)
            {
                case DataType.Boolean:
                    return new VariableCalcurator(src.Bool() != dest.Bool());
                case DataType.Integer:
                    return new VariableCalcurator((src.Int() != 0) != (dest.Int() != 1));
                case DataType.FloatingPoint:
                    return new VariableCalcurator((src.Float() != 0.0f) != (dest.Float() != 0.0f));
                case DataType.UnsighnedInteger:
                    return new VariableCalcurator((src.Uint() != 0) != (dest.Uint() != 0));
                case DataType.Literal:
                    return new VariableCalcurator(string.IsNullOrEmpty(src.Literal()) != string.IsNullOrEmpty(dest.Literal()));
                default:
                    throw new VariableNotDefinedException(string.Format("this '!='is not defined."));
            }
        }

        public static VariableCalcurator operator <=(VariableCalcurator src, VariableCalcurator dest)
        {
            switch (src.DataType)
            {
                //               case TRDataType.Bool:
                //                    return new VariableCalcurator(true);
                case DataType.Integer:
                    return new VariableCalcurator(src.Int() <= dest.Int());
                case DataType.FloatingPoint:
                    return new VariableCalcurator(src.Float() <= dest.Float());
                case DataType.UnsighnedInteger:
                    return new VariableCalcurator(src.Uint() <= dest.Uint());
                //                case TRDataType.Literal:
                //                    return new VariableCalcurator(src.Literal() + dest.Literal());
                default:
                    throw new VariableNotDefinedException(string.Format("this '<='is not defined."));
            }
        }

        public static VariableCalcurator operator >=(VariableCalcurator src, VariableCalcurator dest)
        {
            switch (src.DataType)
            {
                //               case TRDataType.Bool:
                //                    return new VariableCalcurator(true);
                case DataType.Integer:
                    return new VariableCalcurator(src.Int() >= dest.Int());
                case DataType.FloatingPoint:
                    return new VariableCalcurator(src.Float() >= dest.Float());
                case DataType.UnsighnedInteger:
                    return new VariableCalcurator(src.Uint() >= dest.Uint());
                //                case TRDataType.Literal:
                //                    return new VariableCalcurator(src.Literal() + dest.Literal());
                default:
                    throw new VariableNotDefinedException(string.Format("this '>='is not defined."));
            }
        }

        public static VariableCalcurator operator <(VariableCalcurator src, VariableCalcurator dest)
        {
            switch (src.DataType)
            {
                //               case TRDataType.Bool:
                //                    return new VariableCalcurator(true);
                case DataType.Integer:
                    return new VariableCalcurator(src.Int() < dest.Int());
                case DataType.FloatingPoint:
                    return new VariableCalcurator(src.Float() < dest.Float());
                case DataType.UnsighnedInteger:
                    return new VariableCalcurator(src.Uint() < dest.Uint());
                //                case TRDataType.Literal:
                //                    return new VariableCalcurator(src.Literal() + dest.Literal());
                default:
                    throw new VariableNotDefinedException(string.Format("this '<'is not defined."));
            }
        }

        public static VariableCalcurator operator >(VariableCalcurator src, VariableCalcurator dest)
        {
            switch (src.DataType)
            {
                //               case TRDataType.Bool:
                //                    return new VariableCalcurator(true);
                case DataType.Integer:
                    return new VariableCalcurator(src.Int() > dest.Int());
                case DataType.FloatingPoint:
                    return new VariableCalcurator(src.Float() > dest.Float());
                case DataType.UnsighnedInteger:
                    return new VariableCalcurator(src.Uint() > dest.Uint());
                //                case TRDataType.Literal:
                //                    return new VariableCalcurator(src.Literal() + dest.Literal());
                default:
                    throw new VariableNotDefinedException(string.Format("this '>'is not defined."));
            }
        }
    }
}
