// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using OVRSimpleJSON;
using System.Collections.Generic;

namespace Meta.XR.Movement.FaceTracking.Samples
{
    /// <summary>
    /// Provides useful JSON deserialization function that parse face tracking-related
    /// configuration files, or parts of those files.
    /// </summary>
    public static class JSONRigParser
    {
        /// <summary>
        /// Returns a dictionary-of-dictionary collection from provided json.
        /// Assumes that the JSON blob provided comforms to an expected output.
        /// </summary>
        /// <param name="json"></param>
        /// <returns>Dictionary-of-dictionaries.</returns>
        public static Dictionary<string, Dictionary<string, float>> DeserializeV1Mapping(string json)
        {
            var v1Mapping = new Dictionary<string, Dictionary<string, float>>();

            JSONNode rootNode = JSONNode.Parse(json);
            foreach (var jsonNode in rootNode)
            {
                var blendshapeToValue = new Dictionary<string, float>();
                foreach (var keyValuePair in jsonNode.Value)
                {
                    blendshapeToValue.Add(keyValuePair.Key.ToString(), keyValuePair.Value.AsFloat);
                }
                v1Mapping.Add(jsonNode.Key.ToString(), blendshapeToValue);
            }

            return v1Mapping;
        }

        /// <summary>
        /// Returns a list-of-dictionary collection from provided json.
        /// Assumes that the JSON blob provided comforms to an expected output.
        /// </summary>
        /// <param name="json">JSON input.</param>
        /// <returns>List of dictionaries.</returns>
        public static List<Dictionary<string, Dictionary<string, float>>> DeserializeV2Mapping(string json)
        {
            var v2Mapping = new List<Dictionary<string, Dictionary<string, float>>>();

            JSONNode rootNode = JSONNode.Parse(json);
            // The top level is an array of nodes. Below that are collections.
            foreach (var jsonNode in rootNode)
            {
                var stringToCollection = new Dictionary<string, Dictionary<string, float>>();
                foreach (var listItem in jsonNode.Value)
                {
                    string stringKey = listItem.Key.ToString();
                    var blendshapeToValue = new Dictionary<string, float>();
                    foreach (var keyValuePair in listItem.Value)
                    {
                        blendshapeToValue.Add(keyValuePair.Key.ToString(), keyValuePair.Value.AsFloat);
                    }
                    stringToCollection.Add(stringKey, blendshapeToValue);
                }
                v2Mapping.Add(stringToCollection);
            }

            return v2Mapping;
        }
    }
}
