using System.Linq;
using System.Reflection;
using Telerik.JustMock;

namespace Qoollo.Tests.Support
{
    internal static class Accessors
    {
        public static TType GetField<TType>(this object instance, string name) where TType : class
        {
            var accessor = Mock.NonPublic.MakePrivateAccessor(instance);
            return accessor.GetField(name) as TType;
        }

        public static object GetField2(this object instance, string name)
        {
            var accessor = Mock.NonPublic.MakePrivateAccessor(instance);
            return accessor.GetField(name);
        }

        public static object GetFieldPublic(this object instance, string name)
        {
            var fields = instance.GetType().GetProperties();
            foreach (var field in fields)
            {
                if (field.Name == name)
                    return field.GetValue(instance);
            }

            return null;
        }

        public static object GetField(this object instance, string name)
        {
            var accessor = Mock.NonPublic.MakePrivateAccessor(instance);
            return accessor.GetField(name);
        }

        public static TType GetField<TType>(this object instance) where TType : class
        {
            var r = instance.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var field = r.FirstOrDefault(x => x.FieldType.IsSubclassOf(typeof(TType)));
            if (field == null)
                return null;
            return instance.GetField(field.Name) as TType;
        }

        //public static void SetMemeber<TType>(this object instance, string name, TType value) where TType : class
        //{
        //    var accessor = Mock.NonPublic.MakePrivateAccessor(instance);
        //    accessor.SetMember(name, value);
        //}

        public static void SetMemeber(this object instance, string name, object value)
        {
            var accessor = Mock.NonPublic.MakePrivateAccessor(instance);
            accessor.SetMember(name, value);
        }

        public static object GetFieldByAttribute<TAttribute>(this object instance)
        {
            var fields = instance.GetType().GetMembers(BindingFlags.Instance | BindingFlags.Public);

            foreach (var memberInfo in fields)
            {
                var attr = memberInfo.GetCustomAttributes(typeof(TAttribute), true);
                if (attr.Length > 0)
                    return instance.GetField(memberInfo.Name);
            }
            return null;
        }

        public static object GetFieldByInterface<TInterface>(this object instance)
        {
            var fields = instance.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public);
            var filter = fields.FirstOrDefault(x => x.FieldType.GetInterfaces().Contains(typeof(TInterface)));

            return instance.GetField(filter?.Name);
        }
    }
}