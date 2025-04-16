using UnityEngine;

public static class BotManagerStaticUtils
{
    public static Transform[] GetIdleTransformsForProperty(string propertyName, int count)
    {
        var transforms = new Transform[count];

        if (propertyName.Contains("Barn"))
        {
            float startX = 176.6752f;
            float startZ = -18f;
            float y = 0.5f;
            float spacing = 1.2f;
            int columns = 5;

            for (int i = 0; i < count; i++)
            {
                int row = i / columns;
                int col = i % columns;

                float x = startX + col * spacing;
                float z = startZ + row * spacing;

                var idleGO = new GameObject($"IdlePoint_{i}_{propertyName}");
                idleGO.transform.position = new Vector3(x, y, z);

                transforms[i] = idleGO.transform;
            }
        }

        return transforms;
    }
}
