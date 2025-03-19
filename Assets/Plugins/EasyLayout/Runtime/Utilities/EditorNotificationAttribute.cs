namespace EasyLayoutNS
{
	using System;

	/// <summary>
	/// Notification to display after field.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field)]
	public sealed class EditorNotificationAttribute : Attribute
	{
		/// <summary>
		/// Notification type.
		/// </summary>
		public enum NotificationTypes
		{
			/// <summary>
			/// Neutral message.
			/// </summary>			
			None,

			/// <summary>
			/// Info message.
			/// </summary>
			Info,

			/// <summary>
			/// Warning message.
			/// </summary>
			Warning,

			/// <summary>
			/// Error message.
			/// </summary>
			Error,
		}

		readonly string notification;

		/// <summary>
		/// Notification.
		/// </summary>
		public string Notification => notification;

		readonly NotificationTypes notificationType;

		public NotificationTypes NotificationType => notificationType;

		/// <summary>
		/// Initializes a new instance of the <see cref="EditorNotificationAttribute"/> class.
		/// </summary>
		/// <param name="notification">Notification.</param>
		/// <param name="notificationType">Notification type.</param>
		public EditorNotificationAttribute(string notification, NotificationTypes notificationType = NotificationTypes.Info)
		{
			this.notification = notification;
			this.notificationType = notificationType;
		}
	}
}