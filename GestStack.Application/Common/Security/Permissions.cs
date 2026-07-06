using System.Reflection;

namespace GestStack.Application.Common.Security;

public static class Permissions
{
    public static class Users
    {
        public const string Get = "users:get";
        public const string Create = "users:create";
        public const string Modify = "users:modify";
        public const string Delete = "users:delete";

        public static class Roles
        {
            public const string Assign = "users:roles:assign";
        }
    }

    public static class Roles
    {
        public const string Get = "roles:get";
        public const string Create = "roles:create";
        public const string Modify = "roles:modify";
        public const string Delete = "roles:delete";
    }

    public static class Inventory
    {
        public const string Get = "inventory:get";
        public const string Create = "inventory:create";
        public const string Modify = "inventory:modify";
        public const string Delete = "inventory:delete";
    }

    public static class Procurement
    {
        public static class PurchaseRequisitions
        {
            public const string Get = "procurement:pr:get";
            public const string Create = "procurement:pr:create";
            public const string Modify = "procurement:pr:modify";
            public const string Delete = "procurement:pr:delete";
            public const string Approve = "procurement:pr:approve";
        }

        public static class PurchaseOrders
        {
            public const string Get = "procurement:po:get";
            public const string Create = "procurement:po:create";
            public const string Modify = "procurement:po:modify";
            public const string Delete = "procurement:po:delete";
            public const string Approve = "procurement:po:approve";
        }
    }

    public static class Finance
    {
        public const string Get = "finance:get";
        public const string Create = "finance:create";
        public const string Modify = "finance:modify";
        public const string Delete = "finance:delete";
    }

    public static IReadOnlyList<string> All { get; } = [.. Enumerate(typeof(Permissions))];

    private static IEnumerable<string> Enumerate(Type type) =>
        type.GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .Concat(type.GetNestedTypes().SelectMany(Enumerate));
}
