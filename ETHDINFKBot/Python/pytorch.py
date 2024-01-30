import torch
import torchvision.models as models
import torchvision.transforms as transforms
from PIL import Image
from sklearn.metrics.pairwise import cosine_similarity
import numpy as np
import os
import matplotlib.pyplot as plt
import matplotlib.gridspec as gridspec
import cv2
import easyocr

reader = easyocr.Reader(['en', 'de'])


# Load the MobileNetV3_Large model
model = models.mobilenet_v3_large(pretrained=True)

# Modify the model to extract features before the classifier
model = torch.nn.Sequential(*(list(model.children())[:-1]))
model.eval()

# Define the image transforms
transform = transforms.Compose([
    transforms.Resize(256),
    transforms.CenterCrop(224),
    transforms.ToTensor(),
    transforms.Normalize(mean=[0.485, 0.456, 0.406], std=[0.229, 0.224, 0.225]),
])

# Function to get the embedding of an image
def get_embedding(image_path):
    print(image_path)
    image = Image.open(image_path).convert('RGB')
    image = transform(image).unsqueeze(0)
    with torch.no_grad():
        embedding = model(image)
    return embedding.squeeze().numpy()

folder = "memes"

# List of your image paths
image_paths = []
for filename in os.listdir(folder):
    if filename.endswith(".jpg") or filename.endswith(".png"):
        image_paths.append(folder + "/" + filename)
        continue
    else:
        continue

# Extract embeddings
embeddings = [get_embedding(path) for path in image_paths]

# get dimensions of embeddings
print(len(embeddings[0]))
print(embeddings[0])

print(len(embeddings))

# Function to find similar images
def find_similar_images(target_embedding, embeddings, top_k=10):
    similarities = cosine_similarity([target_embedding], embeddings)[0]
    indices = np.argsort(similarities)[::-1][:top_k]
    return indices, similarities[indices]

# Example usage: Find images similar to the first image in the list
index = 0

indices, similarities = find_similar_images(embeddings[index], embeddings)
print(f"Similarities for image {image_paths[index]}")
similar_files = []
for index in indices:
    similar_files.append(image_paths[index])

print(" Files similar: " + str(len(similar_files)))
print(" File names: " + str(similar_files))
print(similarities)
print("")


def easyOCR(reader, imagePath):
    
    # check check if image with _border exists
    # if not create it
    border = 50
    image = cv2.imread(imagePath)
    height = image.shape[0]
    width = image.shape[1]
    
    # add padding to image
    border = 10
    image = cv2.copyMakeBorder(image, border, border, border, border, cv2.BORDER_CONSTANT, value=[0, 0, 0])
    
    # save image
    newPath = imagePath.split('.')[0] + "_border." + imagePath.split('.')[1]
    cv2.imwrite(newPath, image)
    
    results = reader.readtext(newPath)
    print(results)
    
    # draw the text on the output image
    image = cv2.imread(newPath)
    
    # add padding to image
    border = 50
    image = cv2.copyMakeBorder(image, border, border, border, border, cv2.BORDER_CONSTANT, value=[100, 100, 100])
    
    for (bbox, text, prob) in results:
        cv2.rectangle(image, (int(bbox[0][0]) + border, int(bbox[0][1])+ border), (int(bbox[2][0]) + border, int(bbox[2][1]) + border), (0, 255, 0), 2)
        cv2.putText(image, text, (int(bbox[0][0])+ border, int(bbox[0][1] - 10) + border), cv2.FONT_HERSHEY_SIMPLEX, 0.9, (255, 0, 0), 2)
        # put prob on image
        cv2.putText(image, str(round(prob, 2)), (int(bbox[0][0]) + border, int(bbox[0][1] - 30)+ border), cv2.FONT_HERSHEY_SIMPLEX, 0.9, (255, 0, 255), 2)
        
    fullText = ""
    for (bbox, text, prob) in results:
        fullText = fullText + " " + text
    print(fullText)
    # show image with matplotlib
    return (image, fullText)


# Function to display images and their similarities with the desired layout
def display_similar_images(reader, query_path, image_paths, similar_indices, similarities):
    fig_width, fig_height = 10, 5  # dimensions in inches
    fig = plt.figure(figsize=(fig_width, fig_height))
    row = 2
    col = 5    
    count = row * col - row

    gs = gridspec.GridSpec(row, col)  # 2 rows, 5 columns

    # Add subplots
    # Add the large image on the left (1 row, 2 columns, first cell)
    ax1 = plt.subplot(gs[:, 0])


    (reference_image, reference_text) = easyOCR(reader, query_path)

    ax1.imshow(reference_image)
    ax1.axis('off')  # Turn off axis
    ax1.set_title("Query image")
    ax1.set_xlabel(reference_text)


    # Add four smaller images on the right in a 2x2 grid
    # The grid is in the second cell of the 1x2 grid
    for i in range(count):
        x = i % row 
        y = i // row + 1
        print("i: " + str(i) + " x: " + str(x) + " y: " + str(y))
        ax = plt.subplot(gs[x, y])
        (repost_image, repost_text) = easyOCR(reader, image_paths[similar_indices[i]])
        ax.imshow(repost_image)
        ax.axis('off')
        ax.set_title(f"Similarity: {similarities[i]:.2f}")
        ax.set_xlabel(repost_text)
    
    plt.tight_layout()
    plt.show()


query_image_path = image_paths[0]
display_similar_images(reader, query_image_path, image_paths, indices, similarities)
