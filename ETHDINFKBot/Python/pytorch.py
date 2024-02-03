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
import sqlite3


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
model_name = "test"

def ensure_db_exists():
    # check if db exists
    # sqlite db is in repost.db
    if os.path.isfile("repost.db"):
        print("db exists")
    else:
        # create db
        print("db does not exist")
        # create table
        # create db
        conn = sqlite3.connect('repost.db')
        c = conn.cursor()
        
        # todo add img width and height
        # create table: images with columns id, fullfilename, message_id, index, filename, fullpath, extension, filesize, ocrtext
        c.execute('''CREATE TABLE images
                        (id INTEGER PRIMARY KEY AUTOINCREMENT, fullfilename text, message_id integer, file_index integer, filename text, fullpath text, extension text, filesize integer, ocrtext text)''')
        
        # create table models with columns id, name
        c.execute('''CREATE TABLE models
                        (id INTEGER PRIMARY KEY AUTOINCREMENT, name text)''')
        
        # create table embeddings with columns id, image_id, model_id, embedding as blob
        c.execute('''CREATE TABLE embeddings
                        (id INTEGER PRIMARY KEY AUTOINCREMENT, image_id integer, model_id integer, embedding blob)''')
        
        # create table where we store each easyocr box with columns id, image_id, top_left_x, top_left_y, top_right_x, top_right_y, bottom_right_x, bottom_right_y, bottom_left_x, bottom_left_y, text, probability
        c.execute('''CREATE TABLE ocrboxes
                        (id INTEGER PRIMARY KEY AUTOINCREMENT, image_id integer, top_left_x integer, top_left_y integer, top_right_x integer, top_right_y integer, bottom_right_x integer, bottom_right_y integer, bottom_left_x integer, bottom_left_y integer, text text, probability real)''')
        
        conn.commit()
        conn.close()

ensure_db_exists()


def get_model_id(model_name):
    # if model doesnt exist create it
    # save model to db
    conn = sqlite3.connect('repost.db')
    c = conn.cursor()
    model_id = None
    
    # check if model has already been processed
    c.execute("SELECT * FROM models WHERE name=?", (model_name,))
    result = c.fetchone()
    if result is None:
        print("model has not been processed yet " + model_name)
        c.execute("INSERT INTO models (name) VALUES (?)", (model_name,))
        conn.commit()
        model_id = c.lastrowid
    else:
        model_id = result[0]
        
    conn.close()
    return model_id

# List of your image paths
image_paths = []
for filename in os.listdir(folder):
    if filename.endswith(".jpg") or filename.endswith(".png"):
        image_paths.append(folder + "/" + filename)
        continue
    else:
        continue


def create_db_images(paths):
    for path in paths:
        
        # get filename
        fullfilename = path.split('/')[1]
        
        # messageid_index_filename
        #filename is everything after the 2nd _ (it may include _)
        index_of_second_underscore = fullfilename.find('_', fullfilename.find('_') + 1)
        filename = fullfilename[index_of_second_underscore + 1:]
        
        index = fullfilename.split('_')[1]
        index = int(index)
        
        message_id = fullfilename.split('_')[0]
        
        # get fullpath
        fullpath = path
        
        # get extension
        extension = path.split('.')[1]
        
        # get filesize
        filesize = os.path.getsize(path)
        
        # save image to db
        conn = sqlite3.connect('repost.db')
        c = conn.cursor()
        
        # check if image has already been processed
        c.execute("SELECT * FROM images WHERE fullpath=?", (path,))
        result = c.fetchone()
        if result is None:
            print("image has not been processed yet " + path)
            c.execute("INSERT INTO images (fullfilename, message_id, file_index, filename, fullpath, extension, filesize) VALUES (?, ?, ?, ?, ?, ?, ?)", (fullfilename, message_id, index, filename, fullpath, extension, filesize))
            conn.commit()
        
        conn.close()
        
        
create_db_images(image_paths)


# check if embeddings_{mdoel}.npy exists
# if not get embeddings and save them
embeddings = {}
    
for path in image_paths:
    #print(path)
    
    # break on 1000 images
    #if len(embeddings.keys()) > 1000:
    #    break

    # every 500 images print the number of images processed
    if len(embeddings.keys()) % 500 == 0:
        print("images processed: " + str(len(embeddings.keys())))
    
    # save embedding to db
    conn = sqlite3.connect('repost.db')
    c = conn.cursor()
    
    # check if image has already been processed
    c.execute("SELECT * FROM images WHERE fullpath=?", (path,))
    result = c.fetchone()
    if result is None:
        print("image has not been processed yet " + path)
        continue
    

    
    # get image id
    image_id = result[0]

    
    # get model id
    model_id = get_model_id(model_name)
    
    # save embedding to db if it doesnt exist for this image and model
    c.execute("SELECT * FROM embeddings WHERE image_id=? AND model_id=?", (image_id, model_id))
    result = c.fetchone()
    if result is None:
        print("embedding has not been processed yet " + path)
        current_embedding = get_embedding(path)
        
        # convert embedding to blob
        embedding_blob = current_embedding.tobytes()

        embeddings[image_id] = current_embedding
    
        c.execute("INSERT INTO embeddings (image_id, model_id, embedding) VALUES (?, ?, ?)", (image_id, model_id, embedding_blob))
        conn.commit()
    else:
        # add to embeddings dict of img id and embed as valuue
        db_embedding = np.frombuffer(result[3], dtype=np.float32)
        embeddings[image_id] = db_embedding
        
    conn.close()
    
    
# load all embeddings from db for the current model into a dictinary (fullpath, embedding)
def load_embeddings_from_db(model_name):
    # get model id
    model_id = get_model_id(model_name)
    
    # get embeddings from db
    conn = sqlite3.connect('repost.db')
    c = conn.cursor()
    
    # check if image has already been processed
    c.execute("SELECT * FROM embeddings WHERE model_id=?", (model_id,))
    result = c.fetchall()
    
    embeddings = {}
    for row in result:
        # get fullpath
        fullpath = row[2]
        
        # get embedding
        embedding = np.frombuffer(row[3], dtype=np.float32)
        
        embeddings[fullpath] = embedding
        
    conn.close()
    
    return embeddings
    
#embeddings = load_embeddings_from_db(model_name)

# print first 1 embeddings
for key in list(embeddings.keys())[:1]:
    print(key)
    print(embeddings[key])
    print("")

#print number of keys in embeddings
print(len(embeddings.keys()))


# Extract embeddings
#embeddings = [get_embedding(path) for path in image_paths]

# save embeddings to disk
#np.save("embeddings_" + model_name + ".npy", embeddings)
    
# get dimensions of embeddings

# Function to find similar images
def find_similar_images(target_embedding, embeddings_dict, top_k=10):
    # Extract the image IDs and their embeddings from the dictionary
    image_ids = list(embeddings_dict.keys())
    embeddings = np.array(list(embeddings_dict.values()))
    
    # Compute cosine similarities between the target embedding and each of the embeddings in the list
    similarities = cosine_similarity([target_embedding], embeddings)[0]
    
    # Get the indices of the top_k most similar embeddings, sorted by similarity
    indices = np.argsort(similarities)[::-1][:top_k]
    
    # Map the indices back to image IDs
    similar_image_ids = [image_ids[idx] for idx in indices]

    print("similar_image_ids")
    print(similar_image_ids)
    print("")
    print("similarities")
    print(similarities[indices])
    
    # Return the image IDs of the most similar images and their similarities
    return similar_image_ids, similarities[indices]


# Example usage: Find images similar to the first image in the list
# index = 0

# indices, similarities = find_similar_images(embeddings[index], embeddings)
# print(f"Similarities for image {image_paths[index]}")
# similar_files = []
# for index in indices:
#     similar_files.append(image_paths[index])

# print(" Files similar: " + str(len(similar_files)))
# print(" File names: " + str(similar_files))
# print(similarities)
# print("")


def easyOCR(reader, imagePath):
    
    # check in sqlite db if image has already been processed
    # if yes return text
    
    conn = sqlite3.connect('repost.db')
    c = conn.cursor()
    
    # check if the current image has text in the db if not run easyocr
    image_id = None
    c.execute("SELECT * FROM images WHERE fullpath=?", (imagePath,))
    result = c.fetchone()
    if result is None:
        print("image has not been processed yet " + imagePath)
        return
    else:
        #print("image has already been processed " + imagePath)
        image_id = result[0]
        
        # check if the current image has text in the db if not run easyocr
        image_text = result[8]
        
        # if image_text is not None return image_text
        if image_text is not None:
            #print("image has text in db " + imagePath)
            
            # get ocr boxes
            c.execute("SELECT * FROM ocrboxes WHERE image_id=?", (image_id,))
            result = c.fetchall()
            
            # convert boundingbox to numpy array
            image = cv2.imread(imagePath)   

            if result is None:
                print("image has no ocr boxes in db " + imagePath)
                return (image, image_text)
            
            # todo boxes
            
                
            boxes = []
            probabilities = []
            texts = []
            for row in result:
                # get fullpath
                box = np.array([[row[2], row[3]], [row[4], row[5]], [row[6], row[7]], [row[8], row[9]]])
                boxes.append(box)
                probabilities.append(row[11])
                texts.append(row[10])

            
            # draw the text on the output image
            image = cv2.imread(imagePath)

            # add padding to image
            border = 50
            image = cv2.copyMakeBorder(image, border, border, border, border, cv2.BORDER_CONSTANT, value=[100, 100, 100])

            for i in range(len(boxes)):
                bbox = boxes[i]
                text = texts[i]
                prob = probabilities[i]
                cv2.rectangle(image, (int(bbox[0][0]) + border, int(bbox[0][1])+ border), (int(bbox[2][0]) + border, int(bbox[2][1]) + border), (0, 255, 0), 2)
                cv2.putText(image, text, (int(bbox[0][0])+ border, int(bbox[0][1] - 10) + border), cv2.FONT_HERSHEY_SIMPLEX, 0.9, (255, 0, 0), 2)
                # put prob on image
                cv2.putText(image, str(round(prob, 2)), (int(bbox[0][0]) + border, int(bbox[0][1] - 30)+ border), cv2.FONT_HERSHEY_SIMPLEX, 0.9, (255, 0, 255), 2)



            # show image with matplotlib
            return (image, image_text)

            
        

    
    conn.close()
    
    # check check if image with _border exists
    # if not create it
    border = 50
    current_folder = os.getcwd()
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
    
    # delete newPath
    os.remove(newPath)
    
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
    
    # update db with text
    conn = sqlite3.connect('repost.db')
    c = conn.cursor()
    
    # update text for image
    c.execute("UPDATE images SET ocrtext=? WHERE id=?", (fullText, image_id))
    conn.commit()
    
    # save boxes to db
    for (bbox, text, prob) in results:
        c.execute("INSERT INTO ocrboxes (image_id, top_left_x, top_left_y, top_right_x, top_right_y, bottom_right_x, bottom_right_y, bottom_left_x, bottom_left_y, text, probability)"
                  + " VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)", (image_id, int(bbox[0][0]), int(bbox[0][1]), int(bbox[1][0]), int(bbox[1][1]), int(bbox[2][0]), int(bbox[2][1]), int(bbox[3][0]), int(bbox[3][1]), text, prob))
    
    conn.commit()
    conn.close()
    
    # show image with matplotlib
    return (image, fullText)

# go trough all images and run easyocr on them
# save results to db
#for imagePath in image_paths:
    #print("EasyOCR on " + imagePath)
#    easyOCR(reader, imagePath)


# Function to display images and their similarities with the desired layout
def display_similar_images(reader, query_path, image_paths, similar_indices, similarities):
    fig_width, fig_height = 20, 10  # dimensions in inches
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
    ax1.set_title("Query image " + query_path.split('/')[1])
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
        filename = image_paths[similar_indices[i]].split('/')[1]
        ax.set_title(f"Similarity: {similarities[i]:.2f} " + filename)
        ax.set_xlabel(repost_text)
    
    plt.tight_layout()
    # save plot to plot folder

    # check if plot folder exists
    if not os.path.isdir("plots"):
        os.mkdir("plots")
    
    plt.savefig("plots/" + query_path.split('/')[1] + ".png")

    #plt.show()
    
    
 # go trough all embedd combinations and find the ones that have least distance in the top 10
index = 0
count = 0
for key in list(embeddings.keys()):
    indices, similarities = find_similar_images(embeddings[key], embeddings)
    print(f"Similarities for image {key}")
    similar_files = []
    #for index in indices:
        #similar_files.append(image_paths[index])

    print(" Files similar: " + str(len(similar_files)))
    print(" File names: " + str(similar_files))
    print(similarities)
    print("")

    # check if there is a repost
    if similarities[3] > 0.9:
        print("Repost found")
        #print("Query image: " + image_paths[index])
        #print("Repost image: " + image_paths[indices[1]])
        #print("Similarity: " + str(similarities[1]))
        print("")
        display_similar_images(reader, image_paths[index], image_paths, indices, similarities)
        count = count + 1

        if count > 100:
            break
    
    index = index + 1
        


#query_image_path = image_paths[0]
#display_similar_images(reader, query_image_path, image_paths, indices, similarities)
