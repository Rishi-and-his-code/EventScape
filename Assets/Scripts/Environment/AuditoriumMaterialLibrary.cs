using UnityEngine;

/// <summary>
/// Shared material references used by the procedural auditorium builder.
/// Material assets are created and maintained by the Editor scene command.
/// </summary>
[CreateAssetMenu(fileName = "AuditoriumMaterials", menuName = "Virtual Auditorium/Material Library")]
public sealed class AuditoriumMaterialLibrary : ScriptableObject
{
    public Material floor;
    public Material aisle;
    public Material wall;
    public Material ceiling;
    public Material acousticPanel;
    public Material trim;
    public Material column;
    public Material stageWood;
    public Material stageTop;
    public Material backdrop;
    public Material accent;
    public Material door;
    public Material benchWood;
    public Material furnitureMetal;
    public Material tableTop;
    public Material flowerPot;
    public Material furnitureSoil;
    public Material foliage;
    public Material classroomDeskBlue;
    public Material classroomDeskTop;
    public Material classroomPerforation;
    public Material chairBlue;
    public Material chairMetal;

    [Header("Navrachana auditorium architecture")]
    public Material woodHoney;
    public Material woodHoneyLight;
    public Material woodHoneyDark;
    public Material woodWalnut;
    public Material woodWalnutLight;
    public Material stageTimber;
    public Material wallPlaster;
    public Material stoneTile;
    public Material stoneTileLight;
    public Material stoneTileDark;
    public Material structuralMetal;
    public Material acousticPanelLight;
    public Material acousticPanelAlternate;
    public Material acousticFabric;
    public Material projectionScreen;
    public Material rollerBlind;
    public Material speakerFinish;
    public Material speakerGrille;
    public Material fixtureFinish;
    public Material fixtureEmissive;
    public Material bannerRed;
    public Material bannerOrange;
    public Material bannerMagenta;
    public Material bannerArtworkMaterial;
    public Material bannerLeftMaterial;
    public Material bannerRightMaterial;
    public Material rearLogoMaterial;
    public Material podiumWood;
    public Material doorLeaf;
    public Material doorGlass;
    public Material fanMetal;
    public Material fanBlade;

    [Header("Replaceable artwork slots")]
    public Texture2D rearLogoArtwork;
    public Texture2D stageBannerArtwork;
    public Texture2D stageBannerLeftArtwork;
    public Texture2D stageBannerRightArtwork;
}
