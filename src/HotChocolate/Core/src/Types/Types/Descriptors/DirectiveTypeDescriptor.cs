using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Helpers;

namespace HotChocolate.Types.Descriptors
{
    public class DirectiveTypeDescriptor
        : DescriptorBase<DirectiveTypeDefinition>
        , IDirectiveTypeDescriptor
    {
        protected internal DirectiveTypeDescriptor(
            IDescriptorContext context,
            Type clrType)
            : base(context)
        {
            if (clrType is null)
            {
                throw new ArgumentNullException(nameof(clrType));
            }

            Definition.RuntimeType = clrType;
            Definition.Name = context.Naming.GetTypeName(
                clrType, TypeKind.Directive);
            Definition.Description = context.Naming.GetTypeDescription(
                clrType, TypeKind.Directive);
            Definition.IsPublic =
                context.Options.DefaultDirectiveVisibility == DirectiveVisibility.Public;
        }

        protected internal DirectiveTypeDescriptor(IDescriptorContext context)
            : base(context)
        {
            Definition.RuntimeType = typeof(object);
        }

        protected internal DirectiveTypeDescriptor(
            IDescriptorContext context,
            DirectiveTypeDefinition definition)
            : base(context)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        }

        internal protected override DirectiveTypeDefinition Definition { get; protected set; } =
            new DirectiveTypeDefinition();

        protected ICollection<DirectiveArgumentDescriptor> Arguments { get; } =
            new List<DirectiveArgumentDescriptor>();

        protected override void OnCreateDefinition(
            DirectiveTypeDefinition definition)
        {
            if (!Definition.AttributesAreApplied && Definition.RuntimeType != typeof(object))
            {
                Context.TypeInspector.ApplyAttributes(
                    Context,
                    this,
                    Definition.RuntimeType);
                Definition.AttributesAreApplied = true;
            }

            var arguments = new Dictionary<NameString, DirectiveArgumentDefinition>();
            var handledMembers = new HashSet<PropertyInfo>();

            FieldDescriptorUtilities.AddExplicitFields(
                Arguments.Select(t => t.CreateDefinition()),
                f => f.Property,
                arguments,
                handledMembers);

            OnCompleteArguments(arguments, handledMembers);

            definition.Arguments.AddRange(arguments.Values);

            base.OnCreateDefinition(definition);
        }

        protected virtual void OnCompleteArguments(
            IDictionary<NameString, DirectiveArgumentDefinition> arguments,
            ISet<PropertyInfo> handledProperties)
        {
        }

        public IDirectiveTypeDescriptor SyntaxNode(
            DirectiveDefinitionNode directiveDefinitionNode)
        {
            Definition.SyntaxNode = directiveDefinitionNode;
            return this;
        }

        public IDirectiveTypeDescriptor Name(NameString value)
        {
            Definition.Name = value.EnsureNotEmpty(nameof(value));
            return this;
        }

        public IDirectiveTypeDescriptor Description(string value)
        {
            Definition.Description = value;
            return this;
        }

        public IDirectiveArgumentDescriptor Argument(NameString name)
        {
            DirectiveArgumentDescriptor descriptor =
                Arguments.FirstOrDefault(t => t.Definition.Name.Equals(name));
            if (descriptor is { })
            {
                return descriptor;
            }

            descriptor = DirectiveArgumentDescriptor.New(
                Context,
                name.EnsureNotEmpty(nameof(name)));
            Arguments.Add(descriptor);
            return descriptor;
        }

        public IDirectiveTypeDescriptor Location(DirectiveLocation value)
        {
            Array values = Enum.GetValues(typeof(DirectiveLocation));
            foreach (DirectiveLocation item in values)
            {
                if (value.HasFlag(item))
                {
                    Definition.Locations.Add(item);
                }
            }
            return this;
        }

        public IDirectiveTypeDescriptor Use(DirectiveMiddleware middleware)
        {
            if (middleware is null)
            {
                throw new ArgumentNullException(nameof(middleware));
            }

            Definition.MiddlewareComponents.Add(middleware);
            return this;
        }

        public IDirectiveTypeDescriptor Use<TMiddleware>()
            where TMiddleware : class
        {
            return Use(DirectiveClassMiddlewareFactory.Create<TMiddleware>());
        }

        public IDirectiveTypeDescriptor Use<TMiddleware>(
            Func<IServiceProvider, FieldDelegate, TMiddleware> factory)
            where TMiddleware : class
        {
            if (factory is null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            return Use(DirectiveClassMiddlewareFactory.Create(factory));
        }

        [Obsolete("Replace Middleware with `Use`.")]
        public IDirectiveTypeDescriptor Middleware(
            DirectiveMiddleware middleware)
        {
            return Use(middleware);
        }

        [Obsolete("Replace Middleware with `Use`.", true)]
        public IDirectiveTypeDescriptor Middleware<T>(
            Expression<Func<T, object>> method)
        {
            throw new NotSupportedException(
                TypeResources.DirectiveType_ReplaceWithUse);
        }

        [Obsolete("Replace Middleware with `Use`.", true)]
        public IDirectiveTypeDescriptor Middleware<T>(
            Expression<Action<T>> method)
        {
            throw new NotSupportedException(
                TypeResources.DirectiveType_ReplaceWithUse);
        }

        public IDirectiveTypeDescriptor Repeatable()
        {
            Definition.IsRepeatable = true;
            return this;
        }

        public IDirectiveTypeDescriptor Public()
        {
            Definition.IsPublic = true;
            return this;
        }

        public IDirectiveTypeDescriptor Internal()
        {
            Definition.IsPublic = false;
            return this;
        }

        public static DirectiveTypeDescriptor New(
            IDescriptorContext context,
            Type clrType) =>
            new DirectiveTypeDescriptor(context, clrType);

        public static DirectiveTypeDescriptor New(
            IDescriptorContext context) =>
            new DirectiveTypeDescriptor(context);

        public static DirectiveTypeDescriptor<T> New<T>(
            IDescriptorContext context) =>
            new DirectiveTypeDescriptor<T>(context);

        public static DirectiveTypeDescriptor FromSchemaType(
            IDescriptorContext context,
            Type schemaType)
        {
            var descriptor = New(context, schemaType);
            descriptor.Definition.RuntimeType = typeof(object);
            return descriptor;
        }

        public static DirectiveTypeDescriptor From(
            IDescriptorContext context,
            DirectiveTypeDefinition definition) =>
            new DirectiveTypeDescriptor(context, definition);

        public static DirectiveTypeDescriptor From<T>(
            IDescriptorContext context,
            DirectiveTypeDefinition definition) =>
            new DirectiveTypeDescriptor<T>(context, definition);
    }
}
