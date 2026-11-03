using UnityEngine;

/// <summary>
/// Validates furniture using its rotated catalog footprint and physics overlaps.
/// Floor marker colliders and the object being edited are deliberately ignored.
/// </summary>
public static class PlacementValidator
{
    private const float WallClearance = 0.3f;
    private const float CollisionPadding = 0.025f;

    public static string GetInvalidReason(FurnitureInstance candidate, FurnitureType type, Vector3 floorPosition,
        float yawDegrees, AuditoriumDemoSettings settings, Transform placedObjectsRoot,
        FurnitureInstance ignoredObject, Collider[] overlapResults, ref bool physicsStateDirty)
    {
        if (settings == null) return "Auditorium dimensions are unavailable.";
        if (overlapResults == null || overlapResults.Length == 0) return "Collision validation is unavailable.";

        FurnitureDefinition definition = FurnitureCatalog.Get(type);
        Vector3 localScale = candidate != null ? candidate.transform.localScale : Vector3.one;
        Vector3 dimensions = new Vector3(
            definition.Dimensions.x * Mathf.Abs(localScale.x),
            definition.Dimensions.y * Mathf.Abs(localScale.y),
            definition.Dimensions.z * Mathf.Abs(localScale.z));
        bool isStageEquipment = FurnitureCatalog.IsStageEquipment(type);
        bool onStage = isStageEquipment && Mathf.Abs(floorPosition.y - settings.stageHeight) <= 0.05f;
        if (Mathf.Abs(floorPosition.y) > 0.05f && !onStage)
            return "Furniture must rest on the auditorium floor.";
        float halfWidth = dimensions.x * 0.5f;
        float halfDepth = dimensions.z * 0.5f;
        float radians = yawDegrees * Mathf.Deg2Rad;
        float cosine = Mathf.Abs(Mathf.Cos(radians));
        float sine = Mathf.Abs(Mathf.Sin(radians));
        float rotatedHalfWidth = cosine * halfWidth + sine * halfDepth;
        float rotatedHalfDepth = sine * halfWidth + cosine * halfDepth;

        // hallWidth and hallDepth describe the clear interior, not the exterior
        // wall-to-wall size. Keep a small safety margin inside those clear bounds.
        float usableHalfWidth = settings.hallWidth * 0.5f - WallClearance;
        float usableHalfDepth = settings.hallDepth * 0.5f - WallClearance;
        if (Mathf.Abs(floorPosition.x) + rotatedHalfWidth > usableHalfWidth
            || Mathf.Abs(floorPosition.z) + rotatedHalfDepth > usableHalfDepth)
            return "Keep the entire furniture footprint inside the usable floor with wall clearance.";

        // The stage is a fixed no-placement region, checked using its full rectangle.
        float stageHalfWidth = settings.stageWidth * 0.5f;
        float stageHalfDepth = settings.stageDepth * 0.5f;
        if (onStage)
        {
            if (Mathf.Abs(floorPosition.x) + rotatedHalfWidth > stageHalfWidth
                || Mathf.Abs(floorPosition.z - settings.StageCenterZ) + rotatedHalfDepth > stageHalfDepth)
                return "Stage equipment must fit fully on the raised stage platform.";
        }
        else if (Mathf.Abs(floorPosition.x) < stageHalfWidth + rotatedHalfWidth
            && Mathf.Abs(floorPosition.z - settings.StageCenterZ) < stageHalfDepth + rotatedHalfDepth)
        {
            return "Furniture cannot overlap the stage.";
        }

        Vector3 center = floorPosition + Vector3.up * (dimensions.y * 0.5f);
        Bounds candidateWorldBounds = new Bounds(center,
            new Vector3((rotatedHalfWidth + CollisionPadding) * 2f,
                dimensions.y + CollisionPadding * 2f, (rotatedHalfDepth + CollisionPadding) * 2f));
        Vector3 halfExtents = new Vector3(
            halfWidth + CollisionPadding,
            dimensions.y * 0.5f + CollisionPadding,
            halfDepth + CollisionPadding);

        if (physicsStateDirty)
        {
            Physics.SyncTransforms();
            physicsStateDirty = false;
        }

        int overlapCount = Physics.OverlapBoxNonAlloc(center, halfExtents, overlapResults,
            Quaternion.Euler(0f, yawDegrees, 0f), Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < overlapCount; i++)
        {
            Collider other = overlapResults[i];
            if (other == null || !other.enabled) continue;
            // Each compound furniture part has its own collider. Check its world AABB
            // as a cheap broad-phase guard after the rotated physics overlap query.
            if (!candidateWorldBounds.Intersects(other.bounds)) continue;
            if (other.GetComponentInParent<PlacementSurface>() != null) continue;

            FurnitureInstance otherFurniture = other.GetComponentInParent<FurnitureInstance>();
            if (otherFurniture != null)
            {
                if (otherFurniture == candidate || otherFurniture == ignoredObject || otherFurniture.IsPreview) continue;
                if (placedObjectsRoot != null && !otherFurniture.transform.IsChildOf(placedObjectsRoot)) continue;
                return "This position overlaps another placed item.";
            }

            return "This position overlaps a wall or fixed auditorium structure.";
        }

        return null;
    }
}
