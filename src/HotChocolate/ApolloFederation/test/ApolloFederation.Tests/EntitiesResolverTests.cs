using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using Xunit;
using static HotChocolate.ApolloFederation.TestHelper;

namespace HotChocolate.ApolloFederation;

public class EntitiesResolverTests
{
    [Fact(Skip = "Needs to be fixed!")]
    public async void TestResolveViaForeignServiceType()
    {
        // arrange
        ISchema schema = SchemaBuilder.New()
            .AddApolloFederation()
            .AddQueryType<Query>()
            .Create();

        IResolverContext context = CreateResolverContext(schema);

        // act
        var representations = new List<Representation>
        {
            new()
            {
                TypeName = "ForeignType",
                Data = new ObjectValueNode(
                    new ObjectFieldNode("Id", "1"),
                    new ObjectFieldNode("SomeExternalField", "someExternalField"))
            }
        };

        // assert
        List<object?> result = await EntitiesResolver._Entities(schema, representations, context);
        ForeignType obj = Assert.IsType<ForeignType>(result[0]);
        Assert.Equal("1", obj.Id);
        Assert.Equal("someExternalField", obj.SomeExternalField);
        Assert.Equal("InternalValue", obj.InternalField);
    }

    [Fact(Skip = "Needs to be fixed!")]
    public async void TestResolveViaForeignServiceType_MixedTypes()
    {
        // arrange
        ISchema schema = SchemaBuilder.New()
            .AddApolloFederation()
            .AddQueryType<Query>()
            .Create();

        IResolverContext context = CreateResolverContext(schema);

        // act
        var representations = new List<Representation>
        {
            new()
            {
                TypeName = "MixedFieldTypes",
                Data = new ObjectValueNode(
                    new ObjectFieldNode("Id", "1"),
                    new ObjectFieldNode("IntField", 25))
            }
        };

        // assert
        List<object?> result = await EntitiesResolver._Entities(schema, representations, context);
        MixedFieldTypes obj = Assert.IsType<MixedFieldTypes>(result[0]);
        Assert.Equal("1", obj.Id);
        Assert.Equal(25, obj.IntField);
        Assert.Equal("InternalValue", obj.InternalField);
    }

    [Fact]
    public async void TestResolveViaEntityResolver()
    {
        ISchema schema = SchemaBuilder.New()
            .AddApolloFederation()
            .AddQueryType<Query>()
            .Create();

        IResolverContext context = CreateResolverContext(schema);

        // act
        var representations = new List<Representation>
        {
            new()
            {
                TypeName = "TypeWithReferenceResolver",
                Data = new ObjectValueNode(new ObjectFieldNode("Id", "1"))
            }
        };

        // assert
        List<object?> result = await EntitiesResolver._Entities(schema, representations, context);
        TypeWithReferenceResolver obj = Assert.IsType<TypeWithReferenceResolver>(result[0]);
        Assert.Equal("1", obj.Id);
        Assert.Equal("SomeField", obj.SomeField);
    }

    [Fact]
    public async void TestResolveViaEntityResolver_NoTypeFound()
    {
        ISchema schema = SchemaBuilder.New()
            .AddApolloFederation()
            .AddQueryType<Query>()
            .Create();

        IResolverContext context = CreateResolverContext(schema);

        // act
        var representations = new List<Representation>
        {
            new()
            {
                TypeName = "NonExistingTypeName",
                Data = new ObjectValueNode()
            }
        };

        // assert
        Task ShouldThrow() => EntitiesResolver._Entities(schema, representations, context);
        await Assert.ThrowsAsync<SchemaException>(ShouldThrow);
    }

    [Fact]
    public async void TestResolveViaEntityResolver_NoResolverFound()
    {
        ISchema schema = SchemaBuilder.New()
            .AddApolloFederation()
            .AddQueryType<Query>()
            .Create();

        IResolverContext context = CreateResolverContext(schema);

        // act
        var representations = new List<Representation>
        {
            new()
            {
                TypeName = "TypeWithoutRefResolver",
                Data = new ObjectValueNode()
            }
        };

        // assert
        Task ShouldThrow() => EntitiesResolver._Entities(schema, representations, context);
        await Assert.ThrowsAsync<SchemaException>(ShouldThrow);
    }

    public class Query
    {
        public ForeignType ForeignType { get; set; } = default!;
        public TypeWithReferenceResolver TypeWithReferenceResolver { get; set; } = default!;
        public TypeWithoutRefResolver TypeWithoutRefResolver { get; set; } = default!;
        public MixedFieldTypes MixedFieldTypes { get; set; } = default!;
    }

    public class TypeWithoutRefResolver
    {
        public string Id { get; set; } = default!;
    }

    [ReferenceResolver(EntityResolver = nameof(Get))]
    public class TypeWithReferenceResolver
    {
        public string Id { get; set; } = default!;
        public string SomeField { get; set; } = default!;

        public static TypeWithReferenceResolver Get([LocalState] ObjectValueNode data)
        {
            return new TypeWithReferenceResolver
            {
                Id = "1",
                SomeField = "SomeField"
            };
        }
    }

    [ForeignServiceTypeExtension]
    public class ForeignType
    {
        [Key]
        [External]
        public string Id { get; set; } = default!;

        [External]
        public string SomeExternalField { get; set; } = default!;

        public string InternalField { get; set; } = "InternalValue";
    }

    [ForeignServiceTypeExtension]
    public class MixedFieldTypes
    {
        [Key]
        [External]
        public string Id { get; set; } = default!;

        [External]
        public int IntField { get; set; }

        public string InternalField { get; set; } = "InternalValue";
    }
}
