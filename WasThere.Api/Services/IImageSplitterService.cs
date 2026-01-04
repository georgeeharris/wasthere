namespace WasThere.Api.Services;

public interface IImageSplitterService
{
    /// <summary>
    /// Detects individual flyers in an image and returns bounding box information
    /// </summary>
    Task<FlyerBoundingBoxResult> DetectFlyerBoundingBoxesAsync(string imagePath);
    
    /// <summary>
    /// Splits an image into individual flyer images based on bounding boxes
    /// </summary>
    Task<List<string>> SplitImageIntoFlyersAsync(string imagePath, List<BoundingBox> boundingBoxes, string outputDirectory);
}

public class FlyerBoundingBoxResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public List<BoundingBox> BoundingBoxes { get; set; } = new();
    public int FlyerCount => BoundingBoxes.Count;
}

public class BoundingBox
{
    /// <summary>
    /// X coordinate of top-left corner (0-1 normalized)
    /// </summary>
    public float X { get; set; }
    
    /// <summary>
    /// Y coordinate of top-left corner (0-1 normalized)
    /// </summary>
    public float Y { get; set; }
    
    /// <summary>
    /// Width of bounding box (0-1 normalized)
    /// </summary>
    public float Width { get; set; }
    
    /// <summary>
    /// Height of bounding box (0-1 normalized)
    /// </summary>
    public float Height { get; set; }
    
    /// <summary>
    /// Index of this flyer in the image (0-based)
    /// </summary>
    public int Index { get; set; }
}
