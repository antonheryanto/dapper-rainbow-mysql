using System;

namespace Dapper
{
	/// <summary>
	/// Ignore property attribute.
	/// </summary>
	[AttributeUsage (AttributeTargets.Property, AllowMultiple = true)]
	public class IgnorePropertyAttribute : Attribute
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="T:Dapper.IgnorePropertyAttribute"/> class.
		/// </summary>
		/// <param name="ignore">If set to <c>true</c> ignore.</param>
		public IgnorePropertyAttribute(bool ignore)
		{
			Value = ignore;
		}

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="T:Dapper.IgnorePropertyAttribute"/> is value.
		/// </summary>
		/// <value><c>true</c> if value; otherwise, <c>false</c>.</value>
		public bool Value { get; set; }
	}
}