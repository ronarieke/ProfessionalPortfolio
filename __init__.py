import logging
import keras
from keras.models import load_model
import numpy as np
import azure.functions as func
import requests
import io,json

modelSet = {}
convolutional = ["None"]
model = None
appConfig  = None
def main(req: func.HttpRequest) -> func.HttpResponse:
    req_body = req.get_json()
    
    pre = np.array(req_body['numpy'])[:-1]
    granularity = req_body["granularity"]
    logging.info('Python MLFINC {0}.'.format(granularity))
    global modelSet
    global appConfig
    if granularity not in modelSet.keys():
        modelName = 'lstmProd{0}'.format(granularity)
        logging.info(modelName)
        modelSet[granularity] = load_model(io.BytesIO(requests.get(appConfig["config"]["storage"][modelName]).content))
    model = modelSet[granularity]
    pre_x = (pre - np.average(pre,axis=0))/np.std(pre,axis=0)
    pre_x = pre_x.reshape(1,pre_x.shape[0],pre_x.shape[1])
    if granularity in convolutional:
        pre_x = pre_x.reshape(pre_x.shape[0],1,pre_x.shape[1],pre_x.shape[2],1)
    pred = model.predict(pre_x)
    pre_y = np.array([[pre[a][b] for b in range(3,pre.shape[1],5)] for a in range(pre.shape[0])])
    pred = pred[0] * np.std(pre_y,axis=0) +  pre_y[pre.shape[0]-1]
    
    print(pred.shape)
    pred = pred.tolist()

    print(json.dumps(pred))
    return func.HttpResponse(json.dumps(pred))

    if name:
        return func.HttpResponse(f"Hello {name}!")
    else:
        return func.HttpResponse(
            "Please pass a name on the query string or in the request body",
            status_code=400
        )
