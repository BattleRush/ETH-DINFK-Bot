# make flask api

from flask import Flask, request, jsonify

from model import EmbedModel
import easyocr
import cv2


app = Flask(__name__)

reader = easyocr.Reader(['en', 'de'])

# generate embedding 
# parameters model_name, path_to_file
@app.route('/generate_embedding', methods=['POST'])
def generate_embedding():
    model = EmbedModel(512, False)
    model_name = request.form['model_name']
    path_to_file = request.form['path_to_file']

    return jsonify(model.generate_embedding(model_name, path_to_file))

# run ocr on image
# parameters path_to_file
@app.route('/run_ocr', methods=['POST'])
def run_ocr():

    path_to_file = request.form['path_to_file']

    image = cv2.imread(path_to_file)   # TODO check if BGR or RGB is read -> ensure RGB
    #image = cv2.cvtColor(image, cv2.COLOR_BGR2RGB)
    
    # add border to image for better ocr for texts at the edge
    border = 10
    image = cv2.copyMakeBorder(image, border, border, border, border, cv2.BORDER_CONSTANT, value=[0, 0, 0])

    # run ocr
    results = reader.readtext(image)

    return jsonify(results)


if __name__ == '__main__':
    # run on port 13225
    app.run(host="0.0.0.0", port=13225, debug=True)