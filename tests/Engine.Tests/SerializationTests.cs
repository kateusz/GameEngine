using System.Numerics;
using System.Text.Json;
using Bogus;
using Engine.Scene.Serializer;
using Shouldly;
using Xunit;

namespace Engine.Tests;

public class SerializationTests
{
    private readonly Faker _faker = new();
    private readonly JsonSerializerOptions _options;

    public SerializationTests()
    {
        _options = new JsonSerializerOptions
        {
            Converters =
            {
                new Vector2Converter(),
                new Vector3Converter(),
                new Vector4Converter()
            }
        };
    }

    #region Vector2Converter Tests

    [Fact]
    public void Vector2Converter_Serialize_ShouldProduceJsonArray()
    {
        // Arrange
        var vector = new Vector2(1.5f, 2.5f);

        // Act
        var json = JsonSerializer.Serialize(vector, _options);

        // Assert
        json.ShouldBe("[1.5,2.5]");
    }

    [Fact]
    public void Vector2Converter_Deserialize_ShouldReadJsonArray()
    {
        // Arrange
        var json = "[3.14,2.71]";

        // Act
        var vector = JsonSerializer.Deserialize<Vector2>(json, _options);

        // Assert
        vector.X.ShouldBe(3.14f, 0.0001f);
        vector.Y.ShouldBe(2.71f, 0.0001f);
    }

    [Fact]
    public void Vector2Converter_RoundTrip_ShouldPreserveValues()
    {
        // Arrange
        var original = new Vector2(_faker.Random.Float(-100, 100), _faker.Random.Float(-100, 100));

        // Act
        var json = JsonSerializer.Serialize(original, _options);
        var deserialized = JsonSerializer.Deserialize<Vector2>(json, _options);

        // Assert
        deserialized.X.ShouldBe(original.X, 0.0001f);
        deserialized.Y.ShouldBe(original.Y, 0.0001f);
    }

    [Fact]
    public void Vector2Converter_Serialize_WithNaN_ShouldReplaceWithZero()
    {
        // Arrange
        var vector = new Vector2(float.NaN, 5f);

        // Act
        var json = JsonSerializer.Serialize(vector, _options);

        // Assert
        json.ShouldBe("[0,5]");
    }

    [Fact]
    public void Vector2Converter_Serialize_WithInfinity_ShouldReplaceWithZero()
    {
        // Arrange
        var vector = new Vector2(float.PositiveInfinity, float.NegativeInfinity);

        // Act
        var json = JsonSerializer.Serialize(vector, _options);

        // Assert
        json.ShouldBe("[0,0]");
    }

    [Fact]
    public void Vector2Converter_Serialize_WithZero_ShouldSerializeCorrectly()
    {
        // Arrange
        var vector = Vector2.Zero;

        // Act
        var json = JsonSerializer.Serialize(vector, _options);

        // Assert
        json.ShouldBe("[0,0]");
    }

    [Fact]
    public void Vector2Converter_Serialize_WithNegativeValues_ShouldSerializeCorrectly()
    {
        // Arrange
        var vector = new Vector2(-10.5f, -20.3f);

        // Act
        var json = JsonSerializer.Serialize(vector, _options);

        // Assert
        json.ShouldBe("[-10.5,-20.3]");
    }

    #endregion

    #region Vector3Converter Tests

    [Fact]
    public void Vector3Converter_Serialize_ShouldProduceJsonArray()
    {
        // Arrange
        var vector = new Vector3(1f, 2f, 3f);

        // Act
        var json = JsonSerializer.Serialize(vector, _options);

        // Assert
        json.ShouldBe("[1,2,3]");
    }

    [Fact]
    public void Vector3Converter_Deserialize_ShouldReadJsonArray()
    {
        // Arrange
        var json = "[4.5,5.5,6.5]";

        // Act
        var vector = JsonSerializer.Deserialize<Vector3>(json, _options);

        // Assert
        vector.X.ShouldBe(4.5f, 0.0001f);
        vector.Y.ShouldBe(5.5f, 0.0001f);
        vector.Z.ShouldBe(6.5f, 0.0001f);
    }

    [Fact]
    public void Vector3Converter_RoundTrip_ShouldPreserveValues()
    {
        // Arrange
        var original = new Vector3(
            _faker.Random.Float(-100, 100),
            _faker.Random.Float(-100, 100),
            _faker.Random.Float(-100, 100));

        // Act
        var json = JsonSerializer.Serialize(original, _options);
        var deserialized = JsonSerializer.Deserialize<Vector3>(json, _options);

        // Assert
        deserialized.X.ShouldBe(original.X, 0.0001f);
        deserialized.Y.ShouldBe(original.Y, 0.0001f);
        deserialized.Z.ShouldBe(original.Z, 0.0001f);
    }

    [Fact]
    public void Vector3Converter_Serialize_WithNaN_ShouldReplaceWithZero()
    {
        // Arrange
        var vector = new Vector3(1f, float.NaN, 3f);

        // Act
        var json = JsonSerializer.Serialize(vector, _options);

        // Assert
        json.ShouldBe("[1,0,3]");
    }

    [Fact]
    public void Vector3Converter_Serialize_WithInfinity_ShouldReplaceWithZero()
    {
        // Arrange
        var vector = new Vector3(float.PositiveInfinity, 2f, float.NegativeInfinity);

        // Act
        var json = JsonSerializer.Serialize(vector, _options);

        // Assert
        json.ShouldBe("[0,2,0]");
    }

    [Fact]
    public void Vector3Converter_Serialize_WithZero_ShouldSerializeCorrectly()
    {
        // Arrange
        var vector = Vector3.Zero;

        // Act
        var json = JsonSerializer.Serialize(vector, _options);

        // Assert
        json.ShouldBe("[0,0,0]");
    }

    [Fact]
    public void Vector3Converter_Serialize_WithOne_ShouldSerializeCorrectly()
    {
        // Arrange
        var vector = Vector3.One;

        // Act
        var json = JsonSerializer.Serialize(vector, _options);

        // Assert
        json.ShouldBe("[1,1,1]");
    }

    [Fact]
    public void Vector3Converter_Serialize_WithUnitVectors_ShouldSerializeCorrectly()
    {
        // Arrange & Act & Assert
        JsonSerializer.Serialize(Vector3.UnitX, _options).ShouldBe("[1,0,0]");
        JsonSerializer.Serialize(Vector3.UnitY, _options).ShouldBe("[0,1,0]");
        JsonSerializer.Serialize(Vector3.UnitZ, _options).ShouldBe("[0,0,1]");
    }

    #endregion

    #region Vector4Converter Tests

    [Fact]
    public void Vector4Converter_Serialize_ShouldProduceJsonArray()
    {
        // Arrange
        var vector = new Vector4(1f, 2f, 3f, 4f);

        // Act
        var json = JsonSerializer.Serialize(vector, _options);

        // Assert
        json.ShouldBe("[1,2,3,4]");
    }

    [Fact]
    public void Vector4Converter_Deserialize_ShouldReadJsonArray()
    {
        // Arrange
        var json = "[0.1,0.2,0.3,0.4]";

        // Act
        var vector = JsonSerializer.Deserialize<Vector4>(json, _options);

        // Assert
        vector.X.ShouldBe(0.1f, 0.0001f);
        vector.Y.ShouldBe(0.2f, 0.0001f);
        vector.Z.ShouldBe(0.3f, 0.0001f);
        vector.W.ShouldBe(0.4f, 0.0001f);
    }

    [Fact]
    public void Vector4Converter_RoundTrip_ShouldPreserveValues()
    {
        // Arrange
        var original = new Vector4(
            _faker.Random.Float(-100, 100),
            _faker.Random.Float(-100, 100),
            _faker.Random.Float(-100, 100),
            _faker.Random.Float(-100, 100));

        // Act
        var json = JsonSerializer.Serialize(original, _options);
        var deserialized = JsonSerializer.Deserialize<Vector4>(json, _options);

        // Assert
        deserialized.X.ShouldBe(original.X, 0.0001f);
        deserialized.Y.ShouldBe(original.Y, 0.0001f);
        deserialized.Z.ShouldBe(original.Z, 0.0001f);
        deserialized.W.ShouldBe(original.W, 0.0001f);
    }

    [Fact]
    public void Vector4Converter_Serialize_WithNaN_ShouldReplaceWithZero()
    {
        // Arrange
        var vector = new Vector4(1f, 2f, float.NaN, 4f);

        // Act
        var json = JsonSerializer.Serialize(vector, _options);

        // Assert
        json.ShouldBe("[1,2,0,4]");
    }

    [Fact]
    public void Vector4Converter_Serialize_WithInfinity_ShouldReplaceWithZero()
    {
        // Arrange
        var vector = new Vector4(float.PositiveInfinity, 2f, 3f, float.NegativeInfinity);

        // Act
        var json = JsonSerializer.Serialize(vector, _options);

        // Assert
        json.ShouldBe("[0,2,3,0]");
    }

    [Fact]
    public void Vector4Converter_Serialize_WithZero_ShouldSerializeCorrectly()
    {
        // Arrange
        var vector = Vector4.Zero;

        // Act
        var json = JsonSerializer.Serialize(vector, _options);

        // Assert
        json.ShouldBe("[0,0,0,0]");
    }

    [Fact]
    public void Vector4Converter_Serialize_WithOne_ShouldSerializeCorrectly()
    {
        // Arrange
        var vector = Vector4.One;

        // Act
        var json = JsonSerializer.Serialize(vector, _options);

        // Assert
        json.ShouldBe("[1,1,1,1]");
    }

    [Fact]
    public void Vector4Converter_Serialize_WithUnitVectors_ShouldSerializeCorrectly()
    {
        // Arrange & Act & Assert
        JsonSerializer.Serialize(Vector4.UnitX, _options).ShouldBe("[1,0,0,0]");
        JsonSerializer.Serialize(Vector4.UnitY, _options).ShouldBe("[0,1,0,0]");
        JsonSerializer.Serialize(Vector4.UnitZ, _options).ShouldBe("[0,0,1,0]");
        JsonSerializer.Serialize(Vector4.UnitW, _options).ShouldBe("[0,0,0,1]");
    }

    [Fact]
    public void Vector4Converter_Serialize_AsColor_ShouldSerializeCorrectly()
    {
        // Arrange - Simulating RGBA color
        var color = new Vector4(1f, 0.5f, 0.25f, 1f);

        // Act
        var json = JsonSerializer.Serialize(color, _options);

        // Assert
        json.ShouldBe("[1,0.5,0.25,1]");
    }

    #endregion

    #region Edge Cases and Special Values

    [Fact]
    public void VectorConverters_Serialize_WithVerySmallValues_ShouldSerializeCorrectly()
    {
        // Arrange
        var vec2 = new Vector2(0.0001f, 0.0002f);
        var vec3 = new Vector3(0.0001f, 0.0002f, 0.0003f);
        var vec4 = new Vector4(0.0001f, 0.0002f, 0.0003f, 0.0004f);

        // Act
        var json2 = JsonSerializer.Serialize(vec2, _options);
        var json3 = JsonSerializer.Serialize(vec3, _options);
        var json4 = JsonSerializer.Serialize(vec4, _options);

        // Assert
        json2.ShouldContain("0.0001");
        json3.ShouldContain("0.0001");
        json4.ShouldContain("0.0001");
    }

    [Fact]
    public void VectorConverters_Serialize_WithVeryLargeValues_ShouldSerializeCorrectly()
    {
        // Arrange
        var vec2 = new Vector2(10000f, 20000f);
        var vec3 = new Vector3(10000f, 20000f, 30000f);
        var vec4 = new Vector4(10000f, 20000f, 30000f, 40000f);

        // Act
        var json2 = JsonSerializer.Serialize(vec2, _options);
        var json3 = JsonSerializer.Serialize(vec3, _options);
        var json4 = JsonSerializer.Serialize(vec4, _options);

        // Assert - Should handle large values without issues
        JsonSerializer.Deserialize<Vector2>(json2, _options).X.ShouldBe(10000f);
        JsonSerializer.Deserialize<Vector3>(json3, _options).X.ShouldBe(10000f);
        JsonSerializer.Deserialize<Vector4>(json4, _options).X.ShouldBe(10000f);
    }

    [Fact]
    public void VectorConverters_Deserialize_WithExtraWhitespace_ShouldHandleCorrectly()
    {
        // Arrange
        var json2 = "[ 1.0 , 2.0 ]";
        var json3 = "[ 1.0 , 2.0 , 3.0 ]";
        var json4 = "[ 1.0 , 2.0 , 3.0 , 4.0 ]";

        // Act
        var vec2 = JsonSerializer.Deserialize<Vector2>(json2, _options);
        var vec3 = JsonSerializer.Deserialize<Vector3>(json3, _options);
        var vec4 = JsonSerializer.Deserialize<Vector4>(json4, _options);

        // Assert
        vec2.X.ShouldBe(1.0f);
        vec3.X.ShouldBe(1.0f);
        vec4.X.ShouldBe(1.0f);
    }

    [Fact]
    public void Vector2Converter_Serialize_MultipleNaNAndInfinityValues_ShouldReplaceAll()
    {
        // Arrange
        var vector = new Vector2(float.NaN, float.PositiveInfinity);

        // Act
        var json = JsonSerializer.Serialize(vector, _options);

        // Assert
        json.ShouldBe("[0,0]");
    }

    [Fact]
    public void Vector3Converter_Serialize_MultipleNaNAndInfinityValues_ShouldReplaceAll()
    {
        // Arrange
        var vector = new Vector3(float.NaN, float.PositiveInfinity, float.NegativeInfinity);

        // Act
        var json = JsonSerializer.Serialize(vector, _options);

        // Assert
        json.ShouldBe("[0,0,0]");
    }

    [Fact]
    public void Vector4Converter_Serialize_MultipleNaNAndInfinityValues_ShouldReplaceAll()
    {
        // Arrange
        var vector = new Vector4(float.NaN, float.PositiveInfinity, float.NegativeInfinity, float.NaN);

        // Act
        var json = JsonSerializer.Serialize(vector, _options);

        // Assert
        json.ShouldBe("[0,0,0,0]");
    }

    #endregion

    #region Complex Object Serialization

    [Fact]
    public void VectorConverters_InComplexObject_ShouldSerializeCorrectly()
    {
        // Arrange
        var obj = new
        {
            Position = new Vector3(1f, 2f, 3f),
            Color = new Vector4(1f, 0f, 0f, 1f),
            TexCoord = new Vector2(0.5f, 0.5f)
        };

        // Act
        var json = JsonSerializer.Serialize(obj, _options);

        // Assert
        json.ShouldContain("\"Position\":[1,2,3]");
        json.ShouldContain("\"Color\":[1,0,0,1]");
        json.ShouldContain("\"TexCoord\":[0.5,0.5]");
    }

    [Fact]
    public void VectorConverters_ArrayOfVectors_ShouldSerializeCorrectly()
    {
        // Arrange
        var vectors = new[]
        {
            new Vector3(1f, 2f, 3f),
            new Vector3(4f, 5f, 6f),
            new Vector3(7f, 8f, 9f)
        };

        // Act
        var json = JsonSerializer.Serialize(vectors, _options);

        // Assert
        json.ShouldContain("[1,2,3]");
        json.ShouldContain("[4,5,6]");
        json.ShouldContain("[7,8,9]");
    }

    [Fact]
    public void VectorConverters_RoundTrip_WithComplexObject_ShouldPreserveValues()
    {
        // Arrange
        var original = new
        {
            Position = new Vector3(_faker.Random.Float(), _faker.Random.Float(), _faker.Random.Float()),
            Scale = new Vector3(_faker.Random.Float(), _faker.Random.Float(), _faker.Random.Float()),
            Color = new Vector4(_faker.Random.Float(), _faker.Random.Float(), _faker.Random.Float(), _faker.Random.Float())
        };

        // Act
        var json = JsonSerializer.Serialize(original, _options);
        var deserialized = JsonSerializer.Deserialize<JsonElement>(json);

        // Assert - Should produce valid JSON
        json.ShouldNotBeNullOrEmpty();
        deserialized.ValueKind.ShouldNotBe(JsonValueKind.Null);
        deserialized.ValueKind.ShouldNotBe(JsonValueKind.Undefined);
    }

    #endregion
}
