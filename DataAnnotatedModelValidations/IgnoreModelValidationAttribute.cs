using System;

namespace DataAnnotatedModelValidations;

[AttributeUsage(AttributeTargets.Parameter)]
public class IgnoreModelValidationAttribute : Attribute { }
