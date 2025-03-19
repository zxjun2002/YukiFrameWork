namespace EasyLayoutNS.Extensions
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using UnityEngine;

	/// <summary>
	/// Extensions.
	/// </summary>
	public static class EasyLayoutExtensions
	{
		/// <summary>
		/// Is Vector2 has NaN or infinity component?
		/// </summary>
		/// <param name="vector">Vector.</param>
		/// <returns>true if Vector2 has NaN or infinity component; otherwise false.</returns>
		public static bool IsValid(this Vector2 vector) => float.IsNaN(vector.x) || float.IsNaN(vector.y) || float.IsInfinity(vector.x) || float.IsInfinity(vector.y);

		/// <summary>
		/// Is Vector3 has NaN or infinity component?
		/// </summary>
		/// <param name="vector">Vector.</param>
		/// <returns>true if Vector3 has NaN or infinity component; otherwise false.</returns>
		public static bool IsValid(this Vector3 vector) => float.IsNaN(vector.x) || float.IsNaN(vector.y) || float.IsNaN(vector.z)
			|| float.IsInfinity(vector.x) || float.IsInfinity(vector.y) || float.IsInfinity(vector.z);

		/// <summary>
		/// Is List has NaN or infinity value?
		/// </summary>
		/// <param name="values">List.</param>
		/// <param name="index">Index of NaN value.</param>
		/// <returns>true if list has NaN or infinity value; otherwise false.</returns>
		public static bool IsValid(this List<float> values, out int index)
		{
			for (index = 0; index < values.Count; index++)
			{
				if (float.IsNaN(values[index]) || float.IsInfinity(values[index]))
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Is enum value has specified flag?
		/// </summary>
		/// <param name="value">Enum value.</param>
		/// <param name="flag">Flag.</param>
		/// <returns>true if enum has flag; otherwise false.</returns>
		public static bool IsSet(this ResizeType value, ResizeType flag)
		{
			return (value & flag) == flag;
		}

		/// <summary>
		/// Convert the specified input with converter.
		/// </summary>
		/// <param name="input">Input.</param>
		/// <param name="converter">Converter.</param>
		/// <typeparam name="TInput">The 1st type parameter.</typeparam>
		/// <typeparam name="TOutput">The 2nd type parameter.</typeparam>
		/// <returns>List with converted items.</returns>
		public static List<TOutput> Convert<TInput, TOutput>(this List<TInput> input, Converter<TInput, TOutput> converter)
		{
			#if NETFX_CORE
			var output = new List<TOutput>(input.Count);
			for (int i = 0; i < input.Count; i++)
			{
				output.Add(converter(input[i]));
			}
			
			return output;
			#else
			return input.ConvertAll(converter);
			#endif
		}

		/// <summary>
		/// Apply for each item in the list.
		/// </summary>
		/// <param name="source">List.</param>
		/// <param name="action">Action.</param>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		public static void ForEach<T>(this List<T> source, Action<T> action)
		{
			foreach (T element in source)
			{
				action(element);
			}
		}

		/// <summary>
		/// Apply for each item in the list.
		/// </summary>
		/// <param name="source">List.</param>
		/// <param name="action">Action.</param>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		public static void ForEach<T>(this T[] source, Action<T> action)
		{
			foreach (T element in source)
			{
				action(element);
			}
		}

		/// <summary>
		/// Append value.
		/// </summary>
		/// <param name="builder">Builder.</param>
		/// <param name="label">Label.</param>
		/// <param name="value">Value.</param>
		/// <returns>Builder instance.</returns>
		public static StringBuilder AppendValue(this StringBuilder builder, string label, string value)
		{
			builder.Append(label);
			builder.Append(value);
			builder.AppendLine();

			return builder;
		}

		/// <summary>
		/// Append value.
		/// </summary>
		/// <param name="builder">Builder.</param>
		/// <param name="label">Label.</param>
		/// <param name="value">Value.</param>
		/// <returns>Builder instance.</returns>
		public static StringBuilder AppendValue(this StringBuilder builder, string label, bool value)
		{
			builder.Append(label);
			builder.Append(value);
			builder.AppendLine();

			return builder;
		}

		/// <summary>
		/// Append value.
		/// </summary>
		/// <param name="builder">Builder.</param>
		/// <param name="label">Label.</param>
		/// <param name="value">Value.</param>
		/// <returns>Builder instance.</returns>
		public static StringBuilder AppendValue(this StringBuilder builder, string label, int value)
		{
			builder.Append(label);
			builder.Append(value);
			builder.AppendLine();

			return builder;
		}

		/// <summary>
		/// Append value.
		/// </summary>
		/// <param name="builder">Builder.</param>
		/// <param name="label">Label.</param>
		/// <param name="value">Value.</param>
		/// <returns>Builder instance.</returns>
		public static StringBuilder AppendValue(this StringBuilder builder, string label, float value)
		{
			builder.Append(label);
			builder.Append(value);
			builder.AppendLine();

			return builder;
		}

		/// <summary>
		/// Append value.
		/// </summary>
		/// <param name="builder">Builder.</param>
		/// <param name="label">Label.</param>
		/// <param name="value">Value.</param>
		/// <returns>Builder instance.</returns>
		public static StringBuilder AppendValue(this StringBuilder builder, string label, Vector2 value)
		{
			return builder.AppendValue(label, value.ToString());
		}

		/// <summary>
		/// Append value.
		/// </summary>
		/// <param name="builder">Builder.</param>
		/// <param name="label">Label.</param>
		/// <param name="value">Value.</param>
		/// <returns>Builder instance.</returns>
		public static StringBuilder AppendValue(this StringBuilder builder, string label, Vector3 value)
		{
			return builder.AppendValue(label, value.ToString());
		}

		/// <summary>
		/// Append value.
		/// </summary>
		/// <param name="builder">Builder.</param>
		/// <param name="label">Label.</param>
		/// <param name="value">Value.</param>
		/// <returns>Builder instance.</returns>
		public static StringBuilder AppendValue(this StringBuilder builder, string label, Padding value)
		{
			return builder.AppendValue(label, value.ToString());
		}

		/// <summary>
		/// Append value.
		/// </summary>
		/// <param name="builder">Builder.</param>
		/// <param name="label">Label.</param>
		/// <param name="value">Value.</param>
		/// <param name="suffix">Suffix.</param>
		/// <returns>Builder instance.</returns>
		public static StringBuilder AppendValue(this StringBuilder builder, string label, int value, string suffix)
		{
			builder.Append(label);
			builder.Append(value);
			builder.Append(suffix);
			builder.AppendLine();

			return builder;
		}

		/// <summary>
		/// Append value.
		/// </summary>
		/// <param name="builder">Builder.</param>
		/// <param name="label">Label.</param>
		/// <param name="value">Value.</param>
		/// <param name="suffix1">First suffix.</param>
		/// <param name="suffix2">Second suffix.</param>
		/// <returns>Builder instance.</returns>
		public static StringBuilder AppendValue(this StringBuilder builder, string label, int value, string suffix1, string suffix2)
		{
			builder.Append(label);
			builder.Append(value);
			builder.Append(suffix1);
			builder.Append(suffix2);
			builder.AppendLine();

			return builder;
		}

		/// <summary>
		/// Append value.
		/// </summary>
		/// <typeparam name="TEnum">Type of Enum.</typeparam>
		/// <param name="builder">Builder.</param>
		/// <param name="label">Label.</param>
		/// <param name="value">Value.</param>
		/// <returns>Builder instance.</returns>
		public static StringBuilder AppendValueEnum<TEnum>(this StringBuilder builder, string label, TEnum value)
			#if CSHARP_7_3_OR_NEWER
			where TEnum : struct, Enum
			#else
			where TEnum : struct
			#endif
		{
			return builder.AppendValue(label, EnumHelper<TEnum>.Instance.ToString(value));
		}

		#if NETFX_CORE
		/// <summary>
		/// Apply for each item in the list.
		/// </summary>
		/// <param name="input">Input.</param>
		/// <param name="action">Action.</param>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		public static void ForEach<T>(this List<T> list, Action<T> action)
		{
			for (int i = 0; i < list.Count; i++)
			{
				action(list[i]);
			}
		}
		#endif
	}
}