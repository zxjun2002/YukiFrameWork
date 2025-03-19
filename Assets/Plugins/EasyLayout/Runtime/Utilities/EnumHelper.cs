namespace EasyLayoutNS
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Enum helper.
	/// </summary>
	/// <typeparam name="T">Type of the enum.</typeparam>
	public class EnumHelper<T>
#if CSHARP_7_3_OR_NEWER
		where T : struct, Enum
#else
		where T : struct
#endif
	{
		readonly Type EnumType = typeof(T);

		readonly object Sync = new object();

		/// <summary>
		/// Is enum has [Flags] attribute.
		/// </summary>
		public readonly bool IsFlags;

		long[] valuesLong;

		/// <summary>
		/// Values converted to long.
		/// </summary>
		public long[] ValuesLong
		{
			get
			{
				if (valuesLong == null)
				{
					valuesLong = new long[Values.Length];
					for (int i = 0; i < values.Length; i++)
					{
						valuesLong[i] = Convert.ToInt64(values[i]);
					}
				}

				return valuesLong;
			}
		}

		T[] values;

		/// <summary>
		/// Values.
		/// </summary>
		public T[] Values
		{
			get
			{
				values ??= GetValues();

				return values;
			}
		}

		string[] names;

		/// <summary>
		/// Names.
		/// </summary>
		public string[] Names
		{
			get
			{
				names ??= GetNames();

				return names;
			}
		}

		Dictionary<T, string> value2Name;

		/// <summary>
		/// Value to name.
		/// </summary>
		protected Dictionary<T, string> Value2Name
		{
			get
			{
				value2Name ??= GetValue2Name();

				return value2Name;
			}
		}

		bool[] obsolete;

		/// <summary>
		/// Obsolete values.
		/// </summary>
		public bool[] Obsolete
		{
			get
			{
				obsolete ??= GetObsolete();

				return obsolete;
			}
		}

		T[] GetValues()
		{
			lock (Sync)
			{
				return (T[])Enum.GetValues(EnumType);
			}
		}

		bool[] GetObsolete()
		{
			var names = Names;

			lock (Sync)
			{
				var result = new bool[names.Length];
				for (int i = 0; i < names.Length; i++)
				{
					var fi = EnumType.GetField(names[i]);
					var attributes = (ObsoleteAttribute[])fi.GetCustomAttributes(typeof(ObsoleteAttribute), false);
					result[i] = (attributes != null) && (attributes.Length > 0);
				}

				return result;
			}
		}

		string[] GetNames()
		{
			lock (Sync)
			{
				return Enum.GetNames(EnumType);
			}
		}

		Dictionary<T, string> GetValue2Name()
		{
			lock (Sync)
			{
				var result = value2Name;
				if (result != null)
				{
					return result;
				}

				result = new Dictionary<T, string>(Names.Length, EqualityComparer<T>.Default);
				for (int i = 0; i < Values.Length; i++)
				{
					if (!result.ContainsKey(values[i]))
					{
						result.Add(values[i], names[i]);
					}
				}

				return result;
			}
		}

		bool GetIsFlags()
		{
			return EnumType.IsEnum && EnumType.IsDefined(typeof(FlagsAttribute), false);
		}

		/// <summary>
		/// Check is value contains flag.
		/// </summary>
		/// <param name="value">Value.</param>
		/// <param name="flag">Flag.</param>
		/// <returns>true if value contains flag; otherwise false.</returns>
		public bool HasFlag(T value, T flag)
		{
			var value_long = Convert.ToInt64(value);
			var flag_long = Convert.ToInt64(flag);
			return (value_long & flag_long) == flag_long;
		}

		/// <summary>
		/// Convert enum value to the string.
		/// </summary>
		/// <param name="value">Value.</param>
		/// <returns>String representation of the value.</returns>
		public string ToString(T value)
		{
			if (Value2Name.TryGetValue(value, out var name))
			{
				return name;
			}

			// optional: flags version
			// optional: int conversion
			return value.ToString();
		}

		EnumHelper()
		{
			IsFlags = GetIsFlags();
		}

		/// <summary>
		/// Instance.
		/// </summary>
		public static readonly EnumHelper<T> Instance = new EnumHelper<T>();
	}
}