import random
import numpy as np
from keras import models, layers, optimizers, metrics
import json

import azure.cosmos.cosmos_client as cosmos_client
import azure.cosmos.errors as errors
import azure.cosmos.http_constants as http_constants

url = "<COSMOSDBURL>"
key = "<HIDDEN>"

database_id = "<DATABASE>"
container_id = "<CONTAINER>"
client = cosmos_client.CosmosClient(url, {'masterKey': key})

length = 60
step = 15
forward = 15


def fit_one():
    items = client.QueryItems('dbs/'+database_id+'/colls/'+container_id,
                              "SELECT TOP 1 * FROM t", {'enableCrossPartitionQuery': True})
    for item in items:
        dat = json.loads(item['data'])
        n = np.array(dat)
        n = (n-np.average(n, axis=0))/np.std(n, axis=0)
        m = np.array([n[s:s+length]
                      for s in range(0, n.shape[0]-length-forward-1, step)])
        o = np.array([n[s+length:length+s+forward]
                      for s in range(0, n.shape[0]-length-forward-1, step)])
        o = np.array([(np.average(o[a], axis=0)) for a in range(o.shape[0])])
        p = [[o[a][b] for b in range(3, o.shape[1], 5)]
             for a in range(o.shape[0])]
        r = np.array(p)
        return (m.reshape(m.shape[0], m.shape[1], m.shape[2]), r)


records = 2000
epochs = 50
count = 2
iterations = 1


def generate_numpy():
    for x in range(iterations):
        try:
            items = client.QueryItems('dbs/'+database_id+'/colls/'+container_id,
                                      "SELECT TOP {0} * FROM t where t.date < '2018_12_10_16' order by t.date DESC".format(records), {'enableCrossPartitionQuery': True})
            ct = 0
            foo = []
            bar = []
            for item in items:
                dat = json.loads(item['data'])
                n = np.array(dat)
                n = (n-np.average(n, axis=0))/np.std(n, axis=0)
                m = np.array([n[s:s+length]
                              for s in range(0, n.shape[0]-length-forward-1, step)])
                o = np.array([n[s+length:length+s+forward]
                              for s in range(0, n.shape[0]-length-forward-1, step)])
                o = np.array([(np.average(o[a], axis=0))
                              for a in range(o.shape[0])])
                p = [[o[a][b] for b in range(3, o.shape[1], 5)]
                     for a in range(o.shape[0])]
                r = np.array(p)
                if ct == 0:
                    foo = np.array([m])
                    bar = np.array([r])
                else:
                    a = []
                    b = []
                    for aa in range(foo.shape[0]):
                        a.append(foo[aa].tolist())
                        b.append(bar[aa].tolist())
                    a.append(m.tolist())
                    b.append(r.tolist())
                    foo = np.array(a)
                    bar = np.array(b)
                ct += 1
                if ct == count:
                    ct = 0
                    foo = foo.reshape(
                        count*foo.shape[1], foo.shape[2], foo.shape[3])
                    bar = bar.reshape(count*bar.shape[1], bar.shape[2])
                    yield (foo, bar)
        except:
            x -= 1


(m, o) = fit_one()

units = m.shape[1]
model = models.Sequential()
model.add(layers.LSTM(units=units, return_sequences=True, activation="tanh"))
model.add(layers.LSTM(units=units, return_sequences=True, activation="tanh"))
model.add(layers.Dropout(0.1))
model.add(layers.LSTM(units=units, return_sequences=True, activation="tanh"))
model.add(layers.LSTM(units=units, return_sequences=True, activation="tanh"))
model.add(layers.Dropout(0.1))
model.add(layers.LSTM(units=length, activation="tanh"))
model.add(layers.Dense(o.shape[1]))

model.compile(optimizer=optimizers.Adam(),
              loss="mean_squared_error", metrics=["acc"])

model.fit(m, o, epochs=1, batch_size=m.shape[0])


steps_per_epoch = iterations * records / (epochs * count)
model.fit_generator(
    generate_numpy(), steps_per_epoch=steps_per_epoch, epochs=epochs)

model.summary()
model.save('prod_lstm.h5')
