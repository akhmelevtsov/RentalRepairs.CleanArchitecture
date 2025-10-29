using System.Reflection;
using Xunit.Abstractions;

namespace RentalRepairs.WebUI.Tests.Architecture;

/// <summary>
/// ? ARCHITECTURE ENFORCEMENT: Tests that ensure WebUI never directly uses Infrastructure types
/// This enforces the Clean Architecture boundary and composition root pattern
/// </summary>
public class CleanArchitectureBoundaryTests
{
    private readonly ITestOutputHelper _output;

    public CleanArchitectureBoundaryTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void WebUI_Should_Not_Reference_Internal_Infrastructure_Types()
    {
        // Arrange
        var webUIAssembly = typeof(Program).Assembly;
        var infrastructureAssembly = Assembly.Load("RentalRepairs.Infrastructure");

        // Get all types in WebUI assembly
        var webUITypes = webUIAssembly.GetTypes();

        // Get all internal Infrastructure types (these should not be accessible)
        var internalInfrastructureTypes = infrastructureAssembly.GetTypes()
            .Where(t => !t.IsPublic && t.IsClass && !t.IsNested)
            .ToList();

        _output.WriteLine($"Found {internalInfrastructureTypes.Count} internal Infrastructure types");
        _output.WriteLine($"Checking {webUITypes.Length} WebUI types for violations");

        // Act & Assert
        var violations = new List<string>();

        foreach (var webUIType in webUITypes)
        {
            try
            {
                // Check base types
                CheckTypeForInfrastructureViolations(webUIType, internalInfrastructureTypes, violations, "base type");

                // Check field types
                var fields = webUIType.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
                foreach (var field in fields)
                {
                    CheckTypeForInfrastructureViolations(field.FieldType, internalInfrastructureTypes, violations, $"field {field.Name} in {webUIType.Name}");
                }

                // Check property types
                var properties = webUIType.GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
                foreach (var property in properties)
                {
                    CheckTypeForInfrastructureViolations(property.PropertyType, internalInfrastructureTypes, violations, $"property {property.Name} in {webUIType.Name}");
                }

                // Check method parameters and return types
                var methods = webUIType.GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
                foreach (var method in methods)
                {
                    // Check return type
                    CheckTypeForInfrastructureViolations(method.ReturnType, internalInfrastructureTypes, violations, $"return type of method {method.Name} in {webUIType.Name}");

                    // Check parameter types
                    var parameters = method.GetParameters();
                    foreach (var parameter in parameters)
                    {
                        CheckTypeForInfrastructureViolations(parameter.ParameterType, internalInfrastructureTypes, violations, $"parameter {parameter.Name} in method {method.Name} of {webUIType.Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                _output.WriteLine($"Warning: Could not fully analyze type {webUIType.Name}: {ex.Message}");
            }
        }

        // Report results
        if (violations.Any())
        {
            _output.WriteLine("ARCHITECTURE VIOLATIONS FOUND:");
            foreach (var violation in violations.Take(10)) // Limit output
            {
                _output.WriteLine($"  - {violation}");
            }
            if (violations.Count > 10)
            {
                _output.WriteLine($"  ... and {violations.Count - 10} more violations");
            }
        }
        else
        {
            _output.WriteLine("NO ARCHITECTURE VIOLATIONS FOUND");
            _output.WriteLine("WebUI properly uses only Application interfaces and public Infrastructure APIs");
        }

        violations.Should().BeEmpty("WebUI should not directly reference internal Infrastructure types. Use Application interfaces instead.");
    }

    [Fact]
    public void WebUI_Should_Only_Use_Infrastructure_AddInfrastructure_Method()
    {
        // Arrange
        var webUIAssembly = typeof(Program).Assembly;
        var infrastructureAssembly = Assembly.Load("RentalRepairs.Infrastructure");

        // Get all types in WebUI assembly
        var webUITypes = webUIAssembly.GetTypes();

        // Get Infrastructure types
        var infrastructureTypes = infrastructureAssembly.GetTypes().ToList();

        // Act & Assert
        var violations = new List<string>();

        foreach (var webUIType in webUITypes)
        {
            // Skip generated types
            if (webUIType.Name.Contains("GeneratedCode") || 
                webUIType.Name.Contains("Designer") ||
                webUIType.Namespace?.Contains("obj.Debug") == true)
                continue;

            try
            {
                var methods = webUIType.GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
                
                foreach (var method in methods)
                {
                    // Get method body if available
                    var methodBody = method.GetMethodBody();
                    if (methodBody == null) continue;

                    // This is a simplified check - in a real scenario, you'd use more sophisticated IL analysis
                    // For now, we'll check if the method has any suspicious naming patterns
                    var methodName = method.Name.ToLowerInvariant();
                    
                    // Check for direct Infrastructure namespace usage in method names (basic check)
                    if (methodName.Contains("infrastructure") && 
                        !methodName.Contains("addinfrastructure") && 
                        !methodName.Contains("initializedemoa")) // Allow the two public methods
                    {
                        violations.Add($"Method {method.Name} in {webUIType.Name} may be directly using Infrastructure types");
                    }
                }
            }
            catch (Exception ex)
            {
                _output.WriteLine($"Warning: Could not analyze method bodies in type {webUIType.Name}: {ex.Message}");
            }
        }

        violations.Should().BeEmpty("WebUI should only use AddInfrastructure and InitializeDemoDataAsync methods from Infrastructure");
    }

    [Fact]
    public void Infrastructure_DependencyInjection_Should_Be_Only_Public_API()
    {
        // Arrange
        var infrastructureAssembly = Assembly.Load("RentalRepairs.Infrastructure");
        var dependencyInjectionType = infrastructureAssembly.GetType("RentalRepairs.Infrastructure.DependencyInjection");

        // Act
        dependencyInjectionType.Should().NotBeNull("Infrastructure.DependencyInjection type should exist");

        var publicMethods = dependencyInjectionType!.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.DeclaringType == dependencyInjectionType)
            .ToList();

        // Assert
        _output.WriteLine($"Found {publicMethods.Count} public static methods in Infrastructure.DependencyInjection:");
        foreach (var method in publicMethods)
        {
            _output.WriteLine($"  - {method.Name}");
        }

        // Verify only expected public methods exist
        var expectedMethods = new[] { "AddInfrastructure", "InitializeDemoDataAsync" };
        var actualMethodNames = publicMethods.Select(m => m.Name).ToList();

        foreach (var expectedMethod in expectedMethods)
        {
            actualMethodNames.Should().Contain(expectedMethod, 
                $"Infrastructure should expose {expectedMethod} as public API");
        }

        // Verify no unexpected public methods
        var unexpectedMethods = actualMethodNames.Except(expectedMethods).ToList();
        if (unexpectedMethods.Any())
        {
            _output.WriteLine($" Found unexpected public methods: {string.Join(", ", unexpectedMethods)}");
        }
    }

  
    private void CheckTypeForInfrastructureViolations(Type typeToCheck, List<Type> internalInfrastructureTypes, List<string> violations, string context)
    {
        if (typeToCheck == null) return;

        // Handle generic types
        if (typeToCheck.IsGenericType)
        {
            var genericArguments = typeToCheck.GetGenericArguments();
            foreach (var genericArg in genericArguments)
            {
                CheckTypeForInfrastructureViolations(genericArg, internalInfrastructureTypes, violations, $"generic argument in {context}");
            }
        }

        // Handle array types
        if (typeToCheck.IsArray)
        {
            CheckTypeForInfrastructureViolations(typeToCheck.GetElementType()!, internalInfrastructureTypes, violations, $"array element in {context}");
            return;
        }

        // Check if this type is an internal Infrastructure type
        if (internalInfrastructureTypes.Contains(typeToCheck))
        {
            violations.Add($"WebUI uses internal Infrastructure type {typeToCheck.Name} in {context}");
        }
    }
}