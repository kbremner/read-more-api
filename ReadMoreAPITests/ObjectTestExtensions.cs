using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ReadMoreAPITests
{
    public static class ObjectTestExtensions
    {
        public static object GetProperty(this object value, string propertyName)
        {
            dynamic val = value;
            var property = val.GetType().GetProperty(propertyName);
            Assert.IsNotNull(property, $"Value has no property called '{propertyName}'");
            return property.GetValue(val, null);
        }
    }
}
