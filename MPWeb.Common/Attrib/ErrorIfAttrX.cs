using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace MPWeb.Common.Attrib
{

    public enum EvalMode
    {
        [Description("NoOperator")]
        NoOperator,
        [Description("IsTrue")]
        IsTrue,
        [Description("IsFalse")]
        IsFalse,
        [Description("IsFilled")]
        IsFilled,
        [Description("IsEmpty")]
        IsEmpty,
        [Description("IsEqual")]                //kétoperandusú
        IsEqual,
        [Description("IsNotEqual")]             //kétoperandusú
        IsNotEqual,
        [Description("IsBigger")]               //kétoperandusú
        IsBigger,
        [Description("IsBiggerAndNotNull")]     //kétoperandusú
        IsBiggerAndNotNull,
        [Description("IsSmaller")]              //kétoperandusú
        IsSmaller,
        [Description("IsSmallerAndNotNull")]    //kétoperandusú
        IsSmallerAndNotNull,
        [Description("IsSmallerOrEqual")]       //kétoperandusú
        IsSmallerOrEqualr,
        [Description("IsBiggerOrEqual")]        //kétoperandusú
        IsBiggerOrEqual,
        [Description("EvalIsInArray")]
        EvalIsInArray,
        [Description("EvalIsNotInArray")]
        EvalIsNotInArray
    };


    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
    public class    ErrorIfConstAttrX : ValidationAttribute
    {
        private EvalMode m_evalMode;

        private object m_constValue;
        private object[] m_constValues;
        
        private object m_typeId = new object();
        public override object TypeId
        {
            get
            {
                return this.m_typeId;
            }
        }
        
        public ErrorIfConstAttrX(EvalMode p_evalMode, object p_constValues, string p_errMsg)
        {
            m_evalMode = p_evalMode;
            ErrorMessage = p_errMsg;

            if (m_evalMode == EvalMode.EvalIsInArray || m_evalMode == EvalMode.EvalIsInArray)
                m_constValues = p_constValues.ToString().Split(',');
            else
                m_constValue = p_constValues;

        }

        public bool Eval(object p_currValue)
        {
            IEvalValue evalField = null;
            switch (m_evalMode)
            {
                case EvalMode.IsTrue:
                    evalField = new EvalIsTrue();
                    break;
                case EvalMode.IsFalse:
                    evalField = new EvalIsFalse();
                    break;
                case EvalMode.IsFilled:
                    evalField = new EvalIsFilled();
                    break;
                case EvalMode.IsEmpty:
                    evalField = new EvalIsEmpty();
                    break;
                case EvalMode.IsEqual:
                    evalField = new EvalIsEqual(m_constValue);
                    break;
                case EvalMode.IsNotEqual:
                    evalField = new EvalIsNotEqual(m_constValue);
                    break;
                case EvalMode.IsBigger:
                    evalField = new EvalIsBiggerThanAnother(m_constValue);
                    break;
                case EvalMode.IsBiggerAndNotNull:
                    evalField = new EvalIsBiggerThanAnother(m_constValue, false, true);
                    break;
                case EvalMode.IsSmaller:
                    evalField = new EvalIsSmallerThanAnother(m_constValue, false);
                    break;
                case EvalMode.IsSmallerAndNotNull:
                    evalField = new EvalIsSmallerThanAnother(m_constValue, true);
                    break;
                case EvalMode.IsSmallerOrEqualr:
                    evalField = new EvalIsSmallerThanOrEqualAnother(m_constValue);
                    break;
                case EvalMode.IsBiggerOrEqual:
                    evalField = new EvalIsBiggerThanOrEqualAnother(m_constValue);
                    break;
                case EvalMode.EvalIsInArray:
                    evalField = new EvalIsInArray(m_constValues);
                    break;
                case EvalMode.EvalIsNotInArray:
                    evalField = new EvalIsNotInArray(m_constValues);
                    break;
                default:
                    throw new Exception("Unkown EvalMode!");
            }

            return evalField.EvalValue(p_currValue);
        }

        protected override ValidationResult IsValid(object value, ValidationContext context)
        {


            if (Eval(value))
            {
                ValidationResult result = new ValidationResult(String.Format(ErrorMessage, context), new List<string> { context.MemberName });
                return result;
            }
            return ValidationResult.Success;
        }

    }
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
    public class ErrorIfPropAttrX : ValidationAttribute
    {
        private EvalMode m_evalMode;
        private string m_anotherPropName;       //a másik mező neve 

        private object m_typeId = new object();
        public override object TypeId
        {
            get
            {
                return this.m_typeId;
            }
        }
 
        public ErrorIfPropAttrX(EvalMode p_evalMode, String p_anotherPropName, string p_errMsg)
        {
            m_evalMode = p_evalMode;
            m_anotherPropName = p_anotherPropName;
            ErrorMessage = p_errMsg;


        }

        protected override ValidationResult IsValid(object value, ValidationContext context)
        {
            Object instance = context.ObjectInstance;
            Type type = instance.GetType();

            Object currAnotherPropValue = currAnotherPropValue = type.GetProperty(m_anotherPropName).GetValue(instance, null);

            IEvalValue evalField = null;
            switch (m_evalMode)
            {
                case EvalMode.IsEqual:
                    evalField = new EvalIsEqual(currAnotherPropValue);
                    break;
                case EvalMode.IsNotEqual:
                    evalField = new EvalIsNotEqual(currAnotherPropValue);
                    break;
                case EvalMode.IsBigger:
                    evalField = new EvalIsBiggerThanAnother(currAnotherPropValue);
                    break;
                case EvalMode.IsBiggerAndNotNull:
                    evalField = new EvalIsBiggerThanAnother(currAnotherPropValue, false, true);
                    break;
                case EvalMode.IsSmaller:
                    evalField = new EvalIsSmallerThanAnother(currAnotherPropValue, false);
                    break;
                case EvalMode.IsSmallerAndNotNull:
                    evalField = new EvalIsSmallerThanAnother(currAnotherPropValue, true);
                    break;
                case EvalMode.IsSmallerOrEqualr:
                    evalField = new EvalIsSmallerThanOrEqualAnother(currAnotherPropValue);
                    break;
                case EvalMode.IsBiggerOrEqual:
                    evalField = new EvalIsBiggerThanOrEqualAnother(currAnotherPropValue);
                    break;
                default:
                    throw new Exception("Unkown EvalMode!");
            }

            if (evalField.EvalValue(value))
            {
                ValidationResult result = new ValidationResult(String.Format(ErrorMessage, context), new List<string> { context.MemberName });
                return result;
            }
            return ValidationResult.Success;
        }
    }

    /// Összetett validálás, amely az aktuális mező és egy másik mező konstanssal történő validálásából áll
    ///  Értelmezése: 
    ///     1. HA a mező értéke <p_evalMode> a(z) <p_constValue> mező értékével 
    ///        és a(z) <p_anotherPropName> mező értéke <p_anotherEvalMode> a <p_anotherContsValue>  konstanértékkel akkor a mező NEM valid.
    ///        Példa, követeljük meg a számlaszám megadását, ha az átutalás flag be van állíva.
    ///        A 'fizetesimod' mezőre adjuk meg TMRequiredIfComplexAttr  annotációt:
    ///          RequiredIfAttribute( EvalMode.IsEqual, "atutalas", Bankszamla, EvalMode.IsEqual, 
    ///                      "Átutalás esetén a bankszámlaszám megadása kötelező!")
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
    public class ErrorIfComplexAttrX : ValidationAttribute
    {
        private string m_anotherPropName;
        ErrorIfConstAttrX m_currAttr;
        ErrorIfConstAttrX m_anotherAttr;

        private object m_typeId = new object();
        public override object TypeId
        {
            get
            {
                return this.m_typeId;
            }
        }

        public ErrorIfComplexAttrX(EvalMode p_evalMode, object p_constValue, String p_anotherPropName, EvalMode p_anotherEvalMode, object p_anotherContsValue, string p_errMsg)
        {
            ErrorMessage = p_errMsg;
            m_anotherPropName = p_anotherPropName;

            m_currAttr = new ErrorIfConstAttrX( p_evalMode, p_constValue.ToString(), p_errMsg);
            m_anotherAttr = new ErrorIfConstAttrX(p_anotherEvalMode, p_anotherContsValue.ToString(), p_errMsg);


        }

        protected override ValidationResult IsValid(object value, ValidationContext context)
        {
            Object instance = context.ObjectInstance;
            Type type = instance.GetType();


            Object currAnotherPropValue = currAnotherPropValue = type.GetProperty(m_anotherPropName).GetValue(instance, null);

            if (m_currAttr.Eval(value) && m_anotherAttr.Eval(currAnotherPropValue))
            {
                ValidationResult result = new ValidationResult( String.Format(ErrorMessage, context), new List<string> { context.MemberName });
                return result;
            }
            return ValidationResult.Success;
        }
    }
}
