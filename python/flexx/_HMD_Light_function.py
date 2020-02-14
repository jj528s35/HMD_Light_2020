#!/usr/bin/env python
# coding: utf-8

# In[1]:


import numpy as np
import matplotlib.pyplot as plt
import cv2


# In[2]:


def find_targetBlob_pos(img, quad_mask):
    ret = False
    px = 0
    py = 0
    mask = quad_mask.astype(np.uint8)*255
    circles_image = cv2.add(img, np.zeros(np.shape(img), dtype=np.uint8), mask=mask)
    
    # Set up the detector with default parameters.
    detector = cv2.SimpleBlobDetector_create()

    # Detect blobs.
    keypoints = detector.detect(circles_image)

    if len(keypoints) > 0:
        ret = True
        circles_image = cv2.cvtColor(circles_image, cv2.COLOR_GRAY2BGR)
        circles_image = cv2.drawKeypoints(circles_image, keypoints, np.array([]), (0,255,0), cv2.DRAW_MATCHES_FLAGS_DRAW_RICH_KEYPOINTS)
        px = int(keypoints[-1].pt[0])
        py = int(keypoints[-1].pt[1])
        cv2.circle(circles_image, (px,py), 2, (0,0,225), 2)

    
    return ret, px, py, circles_image


# In[3]:


def find_points_on_plane(y, x, mask, kernel_size):
    k = kernel_size//2
    for cy in range(y-k,y+k+1):
        for cx in range(x-k,x+k+1):
            if cx >= 0 and cy >=0 and cx < mask.shape[1] and cy < mask.shape[0] and mask[cy,cx] == True:
                break
                return cx, cy
    
    #if no on the plane ==> 放大kernel size
    kernel_size = kernel_size*2 -1
    k = kernel_size//2
    for cy in range(y-k,y+k+1):
        for cx in range(x-k,x+k+1):
            if cx >= 0 and cy >=0 and cx < mask.shape[1] and cy < mask.shape[0] and mask[cy,cx] == True:
                break
                return cx, cy
    return x, y


# In[4]:


def find_target_circle(grayvalue_img, quad_mask, plane_mask):
    #find circle
    success, px, py, circles_image = find_targetBlob_pos(grayvalue_img, quad_mask)
    if success:
        targetpos = (px, py)
        if(plane_mask[py,px] == False):
            px, py = find_points_on_plane(py, px, plane_mask, kernel_size = 7)
    return success, px, py, circles_image


# In[5]:


# get_ipython().system('jupyter nbconvert --to script _HMD_Light_function.ipynb')

