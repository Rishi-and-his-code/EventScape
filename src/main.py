import cv2
import numpy as np


def run_pipeline():
    print("--- Starting CG IP Baseline Test ---")

    # 1. Create a dummy canvas (300x450 pixels)
    img = np.zeros((300, 450, 3), dtype=np.uint8)

    # 2. Add sample graphics text
    cv2.putText(img, 'CG IP Pipeline is Okay!', (50, 150),
                cv2.FONT_HERSHEY_SIMPLEX, 1, (0, 255, 0), 2)

    # 3. Apply image processing (grayscale and edge detection)
    gray = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY)
    edges = cv2.Canny(gray, 100, 200)

    # 4. Color the detected edges green
    # Create a 3-channel black canvas
    green_edges = np.zeros_like(img)
    green_edges[edges != 0] = (0, 255, 0)

    print("Image processed successfully. ")

    # Save the same frame shown in the window (for submission screenshot)
    cv2.imwrite("docs/screenshots/baseline_output.png", green_edges)

    # 5. Display result
    cv2.imshow('Baseline Test Window', green_edges)
    cv2.waitKey(0)
    cv2.destroyAllWindows()


if __name__ == "__main__":
    run_pipeline()
