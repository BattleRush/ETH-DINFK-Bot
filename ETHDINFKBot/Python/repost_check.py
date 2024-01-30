
from ocr import get_image_text_tesseract, download_image, get_grayscale, thresholding, opening, canny
import cv2
import pytesseract
from pytesseract import Output
import numpy as np
import easyocr
import matplotlib.pyplot as plt
import cv2
from skimage import metrics


reader = easyocr.Reader(['en', 'de'], gpu=False)



#sudo apt install tesseract-ocr
#sudo apt install libtesseract-dev


# get tessaract info







def testOcr(imgName):

    # path is temp / imgName
    path = "temp/" + imgName
    img = cv2.imread(path)
    
    
    height = img.shape[0]
    width = img.shape[1]

    d = pytesseract.image_to_boxes(img, output_type=Output.DICT)
    n_boxes = len(d['char'])
    for i in range(n_boxes):
        (text,x1,y2,x2,y1) = (d['char'][i],d['left'][i],d['top'][i],d['right'][i],d['bottom'][i])
        cv2.rectangle(img, (x1,height-y1), (x2,height-y2) , (0,255,0), 2)
    cv2.imshow('img',img)
    cv2.waitKey(0)
    
    
def improve_bounding_boxes(image_path):
    # Read the image
    image_orig = cv2.imread(image_path)
    
    image = cv2.cvtColor(image_orig, cv2.COLOR_BGR2GRAY)

    # Apply noise reduction
    blurred_image = cv2.GaussianBlur(image, (5, 5), 0)

    # Apply thresholding
    binary_image = cv2.threshold(blurred_image, 127, 255, cv2.THRESH_BINARY | cv2.THRESH_OTSU)[1]

    # Find contours
    contours, _ = cv2.findContours(binary_image, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)

    # Group connected contours into characters
    characters = []
    for contour in contours:
        area = cv2.contourArea(contour)
        if 200 < area < 1000:
            characters.append(contour)

    # Calculate bounding boxes for each character
    bounding_boxes = []
    for character in characters:
        (x, y, w, h) = cv2.boundingRect(character)
        bounding_boxes.append((x, y, x + w, y + h))

    # Apply character width threshold
    width_threshold = 5  # Adjust this value as needed
    filtered_bounding_boxes = []
    for bounding_box in bounding_boxes:
        if abs(bounding_box[2] - bounding_box[0]) > width_threshold:
            filtered_bounding_boxes.append(bounding_box)

    # Apply character aspect ratio filter
    aspect_ratio_threshold = 2.0  # Adjust this value as needed
    final_bounding_boxes = []
    for bounding_box in filtered_bounding_boxes:
        aspect_ratio = bounding_box[2] / bounding_box[3]
        if aspect_ratio >= 0.5 and aspect_ratio <= 2.0:
            final_bounding_boxes.append(bounding_box)
            
    # allow only alphanumeric characters


    # Perform OCR using Tesseract with the refined bounding boxes
    recognized_text = pytesseract.image_to_string(image, config='--oem 3 --psm 11')

    # Print the recognized text
    print(recognized_text)
    
    #draw bounding boxes
    for bounding_box in final_bounding_boxes:
        cv2.rectangle(image_orig, (bounding_box[0], bounding_box[1]), (bounding_box[2], bounding_box[3]), (0, 255, 255), 2)
        # draw the text on the image
        cv2.putText(image_orig, recognized_text, (bounding_box[0], bounding_box[1] - 20), cv2.FONT_HERSHEY_SIMPLEX, 1, (0, 100, 100), 5)
    
    # show image
    cv2.imshow('img',image_orig)
    cv2.waitKey(0)

    

def new_ocr(path):
    # Load the image
    image = cv2.imread(path)

    # Convert to grayscale
    gray = cv2.cvtColor(image, cv2.COLOR_BGR2GRAY)

    # Apply thresholding
    _, thresh = cv2.threshold(gray, 150, 255, cv2.THRESH_BINARY_INV)
    
    # Apply dilation and erosion to remove noise
    kernel = np.ones((1, 1), np.uint8)
    img_dilated = cv2.dilate(thresh, kernel, iterations=1)
    img_eroded = cv2.erode(img_dilated, kernel, iterations=1)

    # OCR with Tesseract (whitelist only alphanumeric characters)
    custom_config = r'--oem 3 --psm 6 -c tessedit_char_whitelist=0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz'
    detections = pytesseract.image_to_data(thresh, config=custom_config, output_type=pytesseract.Output.DICT)

    # Process the results
    n_boxes = len(detections['level'])
    for i in range(n_boxes):
        (x, y, w, h) = (detections['left'][i], detections['top'][i], detections['width'][i], detections['height'][i])
        
        text = detections['text'][i]
        conf = detections['conf'][i]
        text_with_conf = f'{text} ({conf}%)'
        
    
        cv2.rectangle(image, (x, y), (x + w, y + h), (0, 255, 0), 2)
        cv2.putText(image, text_with_conf, (x, y + 10), cv2.FONT_HERSHEY_SIMPLEX, 0.7, (0, 0, 255), 2)
    


    # Display the image with bounding boxes
    cv2.imshow('Image with Bounding Boxes', image)
    cv2.waitKey(0)
    cv2.destroyAllWindows()
        
    
#testOcr("20240129_020010.jpg")
#testOcr("image_4.png")

#improve_bounding_boxes("temp/20240129_020010.jpg")
#improve_bounding_boxes("temp/image_4.png")

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
        
    # show image with matplotlib
    plt.imshow(image)
    plt.show()
    
    
 
#new_ocr("temp/20240129_020010.jpg")
#new_ocr("temp/image_4.png")

#easyOCR(reader, "temp/20240129_020010.jpg")
#easyOCR(reader, "temp/image_4.png")
#easyOCR(reader, "temp/20220614_043525.png")
#easyOCR(reader, "temp/GE8_oCtXgAAdoM1.png")
    
print("done")
    
    
def similarity_test(image1Path, image2Path):

    # Load images
    image1 = cv2.imread(image1Path)
    image2 = cv2.imread(image2Path)
    image2 = cv2.resize(image2, (image1.shape[1], image1.shape[0]), interpolation = cv2.INTER_AREA)
    print(image1.shape, image2.shape)
    # Convert images to grayscale
    image1_gray = cv2.cvtColor(image1, cv2.COLOR_BGR2GRAY)
    image2_gray = cv2.cvtColor(image2, cv2.COLOR_BGR2GRAY)
    # Calculate SSIM
    ssim_score = metrics.structural_similarity(image1_gray, image2_gray, full=True)
    print(f"SSIM Score between image1 {image1Path} and image2 {image2Path} is {round(ssim_score[0], 2)}")
    # SSIM Score: 0.38

images = ["temp/20240129_020010.jpg", "temp/image_4.png", "temp/GE8_oCtXgAAdoM1.png", "temp/20220614_043525.png"]

for i in range(len(images)):
    for j in range(len(images)):
        if i != j:
            similarity_test(images[i], images[j])      

#info1 = get_image_text_tesseract(url1, "frozen_east_text_detection.pb", draw_results=True)
#info2 = get_image_text_tesseract(url2, "frozen_east_text_detection.pb", draw_results=True)


#print(info1)
#print(info2)
