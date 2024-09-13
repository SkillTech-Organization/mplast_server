using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MPWeb.Common.Attrib
{

    public class EvalBase
    {
        protected string getStringValue(object o)
        {
            string sStr = "";

            if (o.GetType() == typeof(DateTime))
            {
                if (o != null)
                    sStr = ((DateTime)(o)).ToString("yyyy.MM.dd HH:mm:ss");
                else
                    sStr = "0000.00.00 00:00:00";
            }
            else
            {
                if (o != null)
                    sStr = o.ToString();
                else
                    sStr = "";
            }
            return sStr;
        }
    }

    public interface IEvalValue
    {
        bool EvalValue(object o);
    }

    public class EvalIsTrue : EvalBase, IEvalValue
    {
        public bool EvalValue(object o)
        {
            return o != null && (bool)o;
        }
    }

    public class EvalIsFalse : EvalBase, IEvalValue
    {
        public bool EvalValue(object o)
        {
            return o == null || !(bool)o;
        }
    }

    public class EvalIsFilled : EvalBase, IEvalValue
    {
        public bool EvalValue(object o)
        {
            return o != null && o.ToString().Length > 0
                && (o.GetType() != typeof(DateTime) || o.ToString() != DateTime.MinValue.ToString());
        }
    }

    public class EvalIsEmpty : EvalBase, IEvalValue
    {
        public bool EvalValue(object o)
        {
            return o == null || o.ToString().Length == 0 ||
                (o.GetType() == typeof(DateTime) && o.ToString() == DateTime.MinValue.ToString());
        }
    }

    public class EvalIsEqual : EvalBase, IEvalValue
    {
        object m_anotherValue = null;
        bool m_inverseCondition = false;
        public EvalIsEqual(object p_anotherValue, bool p_inverseCondition)
        {
            m_anotherValue = p_anotherValue;
            m_inverseCondition = p_inverseCondition;
        }
        public EvalIsEqual(object p_anotherValue)
        {
            m_anotherValue = p_anotherValue;
            m_inverseCondition = false;
        }

        public virtual bool EvalValue(object o)
        {
            string sCurrObj = getStringValue(o);
            string sAnotherObj = getStringValue(m_anotherValue);

            return (sCurrObj.CompareTo(sAnotherObj) == 0) ^ m_inverseCondition;
        }
    }

    public class EvalIsNotEqual : EvalIsEqual
    {
        public EvalIsNotEqual(object p_anotherValue)
            : base(p_anotherValue, true)
        {
        }
    }

    public class EvalIsBiggerThanAnother : EvalBase, IEvalValue
    {
        object m_anotherValue = null;
        bool m_inverseCondition = false;
        bool m_enableNull = false;
        public EvalIsBiggerThanAnother(object p_anotherValue)
        {
            m_anotherValue = p_anotherValue;
            m_inverseCondition = false;
            m_enableNull = false;
        }

        public EvalIsBiggerThanAnother(object p_anotherValue, bool p_inverseCondition)
        {
            m_anotherValue = p_anotherValue;
            m_inverseCondition = p_inverseCondition;
            m_enableNull = false;
        }

        public EvalIsBiggerThanAnother(object p_anotherValue, bool p_inverseCondition, bool p_enableNull)
        {
            m_anotherValue = p_anotherValue;
            m_inverseCondition = p_inverseCondition;
            m_enableNull = p_enableNull;
        }

        public bool EvalValue(object o)
        {
            if (m_enableNull && o == null)
                return !m_inverseCondition;

            string sCurrObj = getStringValue(o);
            string sAnotherObj = getStringValue(m_anotherValue);

            if (m_inverseCondition)                                 //Azért, hogy az == false legyen, nem lehet a compareTo-t csak negálni.
                return (sCurrObj.CompareTo(sAnotherObj) < 0);
            else
                return (sCurrObj.CompareTo(sAnotherObj) > 0);

        }
    }

    public class EvalIsSmallerThanAnother : EvalIsBiggerThanAnother
    {

        public EvalIsSmallerThanAnother(object p_anotherValue, bool p_enableNull)
            : base(p_anotherValue, true, p_enableNull)
        {
        }
    }

    public class EvalIsSmallerThanOrEqualAnother : EvalBase, IEvalValue
    {
        object m_anotherValue = null;
        public EvalIsSmallerThanOrEqualAnother(object p_anotherValue)
        {
            m_anotherValue = p_anotherValue;
        }

        public bool EvalValue(object o)
        {
            string sCurrObj = getStringValue(o);
            string sAnotherObj = getStringValue(m_anotherValue);
            return (sCurrObj.CompareTo(sAnotherObj) <= 0);

        }
    }

    public class EvalIsBiggerThanOrEqualAnother : EvalBase, IEvalValue
    {
        object m_anotherValue = null;
        public EvalIsBiggerThanOrEqualAnother(object p_anotherValue)
        {
            m_anotherValue = p_anotherValue;
        }

        public bool EvalValue(object o)
        {
            string sCurrObj = getStringValue(o);
            string sAnotherObj = getStringValue(m_anotherValue);
            return (sCurrObj.CompareTo(sAnotherObj) >= 0);
        }
    }

    /// <summary>
    /// a mező (sztringesített) értéke megtalálható-e tömbben?
    /// </summary>
    public class EvalIsInArray : IEvalValue
    {
        object[] m_valuesArr = null;
        bool m_inverseCondition = false;
        public EvalIsInArray(object[] p_valuesArr, bool p_inverseCondition)
        {
            m_valuesArr = p_valuesArr;
            m_inverseCondition = p_inverseCondition;
        }
        public EvalIsInArray(object[] p_valuesArr)
            : this(p_valuesArr, false)
        {
        }

        public bool EvalValue(object o)
        {
            if (o == null)
                return false ^ m_inverseCondition;
            if (m_valuesArr == null)
                return false ^ m_inverseCondition;

            return m_valuesArr.Select((s, index) => new { s, index })
                      .Where(x => x.s.ToString() == o.ToString())
                      .Select(x => x.index)
                      .ToList().Count > 0 ^ m_inverseCondition;
        }
    }

    public class EvalIsNotInArray : EvalIsInArray
    {
        public EvalIsNotInArray(object[] p_valuesArr)
            : base(p_valuesArr, true)
        {
        }
    }

}
