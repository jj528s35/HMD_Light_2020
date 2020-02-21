import numpy as np
import matplotlib.pyplot as plt
import cv2
import os
import os.path

def storeimg(imgs, k, fname, string = ''):
    num = len(imgs)
    h = imgs[0].shape[0]
    w = imgs[0].shape[1]
    img_w = w * num
    img_store = np.zeros((h,img_w,3))
    
    for i in range(num):
        if len(imgs[i].shape) == 2:
            image = cv2.cvtColor(imgs[i].astype(np.uint8), cv2.COLOR_GRAY2BGR)
            img_store[:,i*w:(i+1)*w] = image
        else:
            img_store[:,i*w:(i+1)*w] = imgs[i]
    
    TARGETDIR = './Record_data' + '/' + str(fname)
    if not os.path.exists(TARGETDIR):
        os.mkdir(TARGETDIR)
        
    cv2.imwrite(TARGETDIR + '/' + str(string) + str(k).zfill(3) + '.png', img_store)
