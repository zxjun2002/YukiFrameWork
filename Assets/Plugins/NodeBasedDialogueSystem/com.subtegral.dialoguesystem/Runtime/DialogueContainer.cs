using System;
using System.Collections.Generic;
using UnityEngine;

namespace Subtegral.DialogueSystem.DataContainers
{
    [Serializable]
    public class DialogueContainer : ScriptableObject
    {
        public List<NodeLinkData> NodeLinks = new List<NodeLinkData>();
        public List<DialogueNodeData> DialogueNodeData = new List<DialogueNodeData>();
        public List<ExposedProperty> ExposedProperties = new List<ExposedProperty>();
        public List<CommentBlockData> CommentBlockData = new List<CommentBlockData>();

        public void SetValue(string key, string value)
        {
            ExposedProperty property = ExposedProperties.Find(x => x.PropertyName == key);
            if (property == null)
            {
                property = ExposedProperty.CreateInstance();
                property.PropertyName = key;
                ExposedProperties.Add(property);
            }

            property.PropertyValue = value;
        }
    }
}