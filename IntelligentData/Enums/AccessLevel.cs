using System;

namespace IntelligentData.Enums
{
	/// <summary>
	/// The access level for entities.
	/// </summary>
	[Flags]
	public enum AccessLevel
	{
		/// <summary>
		/// Can only read data.
		/// </summary>
		ReadOnly = 0,
		
		/// <summary>
		/// Can insert new records.
		/// </summary>
		Insert = 1,
		
		/// <summary>
		/// Can update existing records.
		/// </summary>
		Update = 2,
		
		/// <summary>
		/// Can delete records.
		/// </summary>
		Delete = 4,
		
		/// <summary>
		/// Can insert, update, and delete records in addition to reading them.
		/// </summary>
		FullAccess = 7
	}
}
