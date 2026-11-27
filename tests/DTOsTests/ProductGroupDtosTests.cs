using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using SeconSbtiBackend.DTOs;

namespace tests.DTOsTests;

public class ProductGroupDtosTests
{
    ///<summary>
    /// Test helper: runs the same validation rules as the API 
    /// and returns the valid results
    /// </summary>
    
    private static List<ValidationResult> Validate(ProductGroupRequest request)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true);
        return results;
    }

    private static List<string> InvalidField(ProductGroupRequest request) =>
        Validate(request).SelectMany(result => result.MemberNames).ToList();

    /**********************ProductGroupResponse*********************/
    [Fact]
    public void ProductGroupResponse_stores_all_values()
    {
        var id = Guid.NewGuid();
        var response = new ProductGroupResponse(id, "Fläktar", "Ventilation");
        response.Id.Should().Be(id);
        response.Name.Should().Be("Fläktar");
        response.Description.Should().Be("Ventilation");
    }

    [Fact]
    public void productGroupResponse_allows_null_description()
    {
        new ProductGroupResponse(Guid.NewGuid(), "Pumpar", null).Description.Should().BeNull();
    }

    [Fact]
    public void ProductGroupResponse_is_equal_when_all_values_are_equal()
    {
        var id = Guid.NewGuid();
        var first = new ProductGroupResponse(id, "Fläktar", "Ventilation");
        var second = new ProductGroupResponse(id, "Fläktar", "Ventilation");

        first.Should().Be(second);
        (first == second).Should().BeTrue();
        first.GetHashCode().Should().Be(second.GetHashCode());
    }

    [Fact]
    public void ProductGroupResponse_is_not_equal_when_a_value_differs()
    {
        var first = new ProductGroupResponse(Guid.NewGuid(), "Fläktar", null);
        var second = first with { Name = "Pumpar" };

        first.Should().NotBe(second);
        (first == second).Should().BeFalse();
        second.Name.Should().Be("Pumpar");
    }

    [Fact]
    public void ProductGroupResponse_ToString_contains_type_name_and_values()
    {
        var text = new ProductGroupResponse(Guid.NewGuid(), "Fläktar", "Ventilation").ToString();

        text.Should().Contain(nameof(ProductGroupResponse));
        text.Should().Contain("Fläktar");
        text.Should().Contain("Ventilation");
    }

    [Fact]
    public void ProductGroupResponse_can_be_deconstructed()
    {
        var id = Guid.NewGuid();
        var (deconstructedId, name, description) = new ProductGroupResponse(id, "Fläktar", "Ventilation");
        deconstructedId.Should().Be(id);
        name.Should().Be("Fläktar");
        description.Should().Be("Ventilation");
    }

    /************************ProductGroupRequest: defaults and values************************/
    [Fact]
    public void ProductGroupRequest_has_null_values_by_default()
    {
        var request = new ProductGroupRequest();
        request.Name.Should().BeNull();
        request.Description.Should().BeNull();
    }

    [Fact]
    public void ProductGroupRequest_stores_values()
    {
        var request = new ProductGroupRequest { Name = "Fläktar", Description = "Ventilation" };
        request.Name.Should().Be("Fläktar");
        request.Description.Should().Be("Ventilation");
    }

    /************************ProductGroupRequest: Name************************/
    [Fact]
    public void ProductGroupRequest_valid_request_has_no_errors()
    {
        Validate(new ProductGroupRequest { Name = "Fläktar", Description = "Ventilation" }).Should().BeEmpty();
    }

    [Fact]
    public void ProductGroupRequest_description_is_optional()
    {
        Validate(new ProductGroupRequest { Name = "Fläktar" }).Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ProductGroupRequest_missing_name_is_rejected_with_message(string? name)
    {
        var results = Validate(new ProductGroupRequest { Name = name });
        var error = results.Should().ContainSingle().Subject;
        error.MemberNames.Should().Equal(nameof(ProductGroupRequest.Name));
        error.ErrorMessage.Should().Be("Ange ett namn på produktgruppen.");
    }

    [Fact]
    public void ProductGroupRequest_name_with_100_characters_is_valid()
    {
        Validate(new ProductGroupRequest { Name = new string('a', 100) }).Should().BeEmpty();
    }

    [Fact]
    public void ProductGroupRequest_name_with_101_characters_is_rejected_with_message()
    {
        var results = Validate(new ProductGroupRequest { Name = new string('a', 101) });

        var error = results.Should().ContainSingle().Subject;
        error.MemberNames.Should().Equal(nameof(ProductGroupRequest.Name));
        error.ErrorMessage.Should().Be("Namnet får vara högst 100 tecken.");
    }

    /************************ProductGroupRequest: Description************************/
    [Fact]
    public void ProductGroupRequest_description_with_500_characters_is_valid()
    {
        Validate(new ProductGroupRequest { Name = "Fläktar", Description = new string('a', 500) }).Should().BeEmpty();
    }

    [Fact]
    public void ProductGroupRequest_description_with_501_characters_is_rejected_with_message()
    {
        var results = Validate(new ProductGroupRequest { Name = "Fläktar", Description = new string('a', 501) });
        var error = results.Should().ContainSingle().Subject;
        error.MemberNames.Should().Equal(nameof(ProductGroupRequest.Description));
        error.ErrorMessage.Should().Be("Beskrivningen får vara högst 500 tecken.");
    }

    [Fact]
    public void ProductGroupRequest_reports_both_fields_when_both_are_invalid()
    {
        InvalidField(new ProductGroupRequest { Name = new string('a', 101), Description = new string('b', 501) })
        .Should().BeEquivalentTo(nameof(ProductGroupRequest.Name), nameof(ProductGroupRequest.Description));
    }

}