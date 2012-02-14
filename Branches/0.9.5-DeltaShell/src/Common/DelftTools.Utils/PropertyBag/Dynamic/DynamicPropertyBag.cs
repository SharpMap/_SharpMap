using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using DelftTools.Utils.ComponentModel;

namespace DelftTools.Utils.PropertyBag.Dynamic
{
    /// <summary>
    /// Creates a custom type descriptor for an object using reflection as a property bag. Used for Property Grid.
    /// Additionally it scans the object for any dynamic attributes and processes those, eg checks their condition
    /// at runtime and if met, adds them as static attribute.
    /// </summary>
    public class DynamicPropertyBag : PropertyBag
    {
        private readonly object propertyObject;

        public DynamicPropertyBag(object propertyObject)
        {
            this.propertyObject = propertyObject;

            var properties = propertyObject.GetType().GetProperties();

            foreach(PropertyInfo propertyInfo in properties)
            {
                PropertySpec propertySpec = GetProperySpecForProperty(propertyInfo);
                Properties.Add(propertySpec);
            }
        }

        private static PropertySpec GetProperySpecForProperty(PropertyInfo propertyInfo)
        {
            var propertySpec = new PropertySpec(propertyInfo.Name, propertyInfo.PropertyType);
                
            var attributeList = new List<Attribute>();
            foreach(object attrib in propertyInfo.GetCustomAttributes(true))
                if (attrib is Attribute)
                    attributeList.Add(attrib as Attribute);

            if (propertyInfo.GetSetMethod()==null) 
            {
                attributeList.Add(new ReadOnlyAttribute(true));
            }
            propertySpec.Attributes = attributeList.ToArray();
            
            return propertySpec;
        }

        protected override void OnGetValue(PropertySpecEventArgs e)
        {
            base.OnGetValue(e);

            var attributeList = new List<Attribute>();
            attributeList.AddRange(e.Property.Attributes.ToList());

            MethodInfo validationMethod = null;

            //check all of the attributes: if we find a dynamic one, evaluate it and possibly add/overwrite a static attribute
            foreach (Attribute customAttribute in e.Property.Attributes)
            {
                if (customAttribute is DynamicReadOnlyAttribute)
                {
                    if(validationMethod == null)
                    {
                        validationMethod = GetDynamicReadOnlyValidationMethod(propertyObject);
                    } 
                    
                    attributeList.RemoveAll(x => x.GetType() == typeof(ReadOnlyAttribute));

                    // check if property should be read-only
                    if (validationMethod == null)
                    {
                        throw new MissingMethodException(
                            string.Format("{0} uses DynanamicReadOnlyAttribute but does not have method marked using DynamicReadOnlyValidationMethodAttribute", propertyObject));
                    }

                    var shouldBeReadOnly = (bool) validationMethod.Invoke(propertyObject, new [] { e.Property.Name });

                    if (shouldBeReadOnly) //invoke the method
                    {
                        //condition is true: the dynamic attribute should be applied (as static attribute)
                        attributeList.Add(new ReadOnlyAttribute(true)); //add static read only attribute
                    }
                }
            }
            
            e.Property.Attributes = attributeList.ToArray();

            e.Value = propertyObject.GetType().GetProperty(e.Property.Name).GetValue(propertyObject, null);
        }

        private static MethodInfo GetDynamicReadOnlyValidationMethod(object o)
        {
            var type = o.GetType();
            var validationMethods = type.GetMethods().Where(methodInfo => methodInfo.GetCustomAttributes(false).Any(a => a is DynamicReadOnlyValidationMethodAttribute));

            if (validationMethods.Count() == 0)
            {
                throw new MissingMethodException("DynamicReadOnlyValidationMethod not found (or not public), class: " + type);
            }

            if (validationMethods.Count() > 1)
            {
                throw new MissingMethodException("Only one DynamicReadOnlyValidationMethod is allowed per class: " + type);
            }

            var validationMethod = validationMethods.FirstOrDefault();

            // check return type and arguments
            if(validationMethod.ReturnType != typeof(bool))
            {
                throw new MissingMethodException("DynamicReadOnlyValidationMethod must use bool as a return type, class: " + type);
            }

            if(validationMethod.GetParameters().Length != 1)
            {
                throw new MissingMethodException("DynamicReadOnlyValidationMethod has incorrect number of arguments, should be 1 of type string, class: " + type);
            }

            if (validationMethod.GetParameters()[0].ParameterType != typeof(string))
            {
                throw new MissingMethodException("DynamicReadOnlyValidationMethod has incorrect argument type, should be of type string, class: " + type);
            }

            return validationMethod;
        }

        protected override void OnSetValue(PropertySpecEventArgs e)
        {
            base.OnSetValue(e);

            propertyObject.GetType().GetProperty(e.Property.Name).SetValue(propertyObject,e.Value,null);
        }

        public Type GetContentType()
        {
            return propertyObject.GetType();
        }
    }
}
