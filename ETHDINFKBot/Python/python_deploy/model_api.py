# make flask api

from flask import Flask, request, jsonify
from model import EmbedModel
import easyocr
import cv2
#from PIL import Image
#import torch

app = Flask(__name__)
reader = easyocr.Reader(['en', 'de'])



#def get_embedding(model, transform, image_path):
#    print("Generating embedding for: " + image_path)
#    image = Image.open(image_path).convert('RGB')
#    image = transform(image).unsqueeze(0)
#    with torch.no_grad():
#        embedding = model(image)
#    return embedding.squeeze().numpy()

# generate embedding 
# parameters model_name, path_to_file
#@app.route('/generate_embedding', methods=['POST'])
#def generate_embedding():
#    content = request.json
#    print(content['model_name'])
#    print(content['path_to_file'])
#    model_name = content['model_name']
#    path_to_file = content['path_to_file']

    
#    model = EmbedModel(model_name=model_name)

#    result = get_embedding(model, model.transform, path_to_file)

#    print(result)

#    return jsonify(result)

# run ocr on image
# parameters path_to_file
@app.route('/run_ocr', methods=['POST'])
def run_ocr():
    # print body of request
    #print("JSON " + str(request.json))

    content = request.json
    print(content['path_to_file'])
    path_to_file = content['path_to_file']

    print("Path to file: " + path_to_file)

    image = cv2.imread(path_to_file)   # TODO check if BGR or RGB is read -> ensure RGB
    #image = cv2.cvtColor(image, cv2.COLOR_BGR2RGB)
    
    # add border to image for better ocr for texts at the edge
    border = 10
    image = cv2.copyMakeBorder(image, border, border, border, border, cv2.BORDER_CONSTANT, value=[0, 0, 0])

    # run ocr
    results = reader.readtext(image)

    print(results)

    # process results into json
    # structure of json:
    # {
    #   "text": "text",
    #   "coordinates": {
    #       "top_left": [x, y],
    #       "top_right": [x, y],
    #       "bottom_left": [x, y],
    #       "bottom_right": [x, y]
    #   },
    #   "confidence": confidence
    # }

    results_json = []

    # ensure that the int array is serializable and doenst throw TypeError: Object of type int32 is not JSON serializable
    for result in results:
        result_json = {
            "text": result[1],
            "coordinates": {
                "top_left": [int(result[0][0][0]) - 10, int(result[0][0][1]) - 10],
                "top_right": [int(result[0][1][0]) - 10, int(result[0][1][1]) - 10],
                "bottom_left": [int(result[0][3][0]) - 10, int(result[0][3][1]) - 10],
                "bottom_right": [int(result[0][2][0]) - 10, int(result[0][2][1]) - 10]
            },
            "confidence": result[2]
        }

        results_json.append(result_json)

    print(results_json)

    return jsonify(results_json)


#RESTART endpoint which stops the application
@app.route('/restart', methods=['GET'])
def restart():
    # kill current process
    print("Restarting application")
    exit()

if __name__ == '__main__':
    # run on port 13225
    app.run(host="0.0.0.0", port=13225, debug=True)