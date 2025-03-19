namespace EasyLayoutNS
{
	using System.Collections.Generic;
	using UnityEngine;

	/// <summary>
	/// Hex pool.
	/// </summary>
	public class HexPool
	{
		/// <summary>
		/// Instances.
		/// </summary>
		protected Dictionary<CubeCoordinates, HexCoordinates> instances = new Dictionary<CubeCoordinates, HexCoordinates>();

		/// <summary>
		/// Instances.
		/// </summary>
		public IReadOnlyDictionary<CubeCoordinates, HexCoordinates> Instances => instances;

		/// <summary>
		/// Templates cache.
		/// </summary>
		protected Dictionary<int, Stack<HexCoordinates>> TemplatesCache = new Dictionary<int, Stack<HexCoordinates>>();

		/// <summary>
		/// Template cache.
		/// </summary>
		protected Stack<HexCoordinates> TemplateCache;

		/// <summary>
		/// Current template.
		/// </summary>
		protected HexCoordinates Template;

		/// <summary>
		/// Current template ID.
		/// </summary>
		protected int TemplateId = 0;

		/// <summary>
		/// Container.
		/// </summary>
		protected Transform Container;

		/// <summary>
		/// Initializes a new instance of the <see cref="HexPool"/> class.
		/// </summary>
		/// <param name="container">Container.</param>
		public HexPool(Transform container)
		{
			Container = container;
		}

		/// <summary>
		/// Clear.
		/// </summary>
		protected virtual void Clear()
		{
			if (Template == null)
			{
				return;
			}

			foreach (var instance in instances.Values)
			{
				instance.gameObject.SetActive(false);
				TemplateCache.Push(instance);
			}

			instances.Clear();
		}

		/// <summary>
		/// Set template.
		/// </summary>
		/// <param name="newTemplate">New template.</param>
		public virtual void SetTemplate(HexCoordinates newTemplate)
		{
			Clear();

			if (TemplateId != newTemplate.GetInstanceID())
			{
				TemplateId = newTemplate.GetInstanceID();
				Template = newTemplate;
				if (!TemplatesCache.TryGetValue(TemplateId, out TemplateCache))
				{
					TemplateCache = new Stack<HexCoordinates>();
					TemplatesCache[TemplateId] = TemplateCache;
				}
			}
		}

		/// <summary>
		/// Get instance.
		/// </summary>
		/// <param name="coordinates">Coordinates.</param>
		/// <returns>Instance.</returns>
		public virtual HexCoordinates GetInstance(OffsetCoordinates coordinates)
		{
			var instance = TemplateCache.Count > 0
				? TemplateCache.Pop()
				: UnityEngine.Object.Instantiate(Template, Container);

			instance.SetCoordinates(coordinates);
			instances[instance.Cube] = instance;
			instance.gameObject.SetActive(true);

			return instance;
		}
	}
}