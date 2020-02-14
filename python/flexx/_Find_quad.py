#!/usr/bin/env python
# coding: utf-8

# In[1]:


import numpy as np
import matplotlib.pyplot as plt
import cv2
import random
import math
from matplotlib.path import Path


# In[2]:


def MorphologyEx(img):
    kernel_size = 5 #7
    kernel = cv2.getStructuringElement(cv2.MORPH_RECT,(kernel_size, kernel_size))
    #膨胀之后再腐蚀，在用来关闭前景对象里的小洞或小黑点
    #开运算用于移除由图像噪音形成的斑点
    opened = cv2.morphologyEx(img, cv2.MORPH_OPEN, kernel)
    closing = cv2.morphologyEx(img,cv2.MORPH_CLOSE,kernel)
    
    kernel_size = 3
    plane_blur = cv2.GaussianBlur(closing,(kernel_size, kernel_size), 0)
    return plane_blur


def find_quadrilateral(img):
    #find large 四邊形 on the mask of plane, and find the center of it
    HoughLines_edge = np.zeros((img.shape[0],img.shape[1]))
    cx = 0
    cy = 0
    approx = []
    ret = False
    
    #关闭前景对象里的小洞或小黑点
    blur = MorphologyEx(img)
    image = cv2.cvtColor(blur, cv2.COLOR_GRAY2BGR)
    
    #find Contours with large area
    (_, cnts, _) = cv2.findContours(blur, cv2.RETR_LIST, cv2.CHAIN_APPROX_SIMPLE)
    cnts=sorted(cnts, key = cv2.contourArea, reverse = True)[:5]

    # loop over our contours
    for c in cnts:
        peri = cv2.arcLength(c, True)#輪廓長度
        area = cv2.contourArea(c)
        
        approx = cv2.approxPolyDP(c, 0.08 * peri, True)#把一个连续光滑曲线折线化
        approx = cv2.convexHull(approx)# find凸多邊形框
        if area > 500:
            approx = cv2.approxPolyDP(c, 0.08 * peri, True)#把一个连续光滑曲线折线化
            approx = cv2.convexHull(approx)# find凸多邊形框
            if len(approx) == 4 :
                cv2.drawContours(image, [approx], -1, (255,0,0), 3)
                break
            elif len(approx) > 4:
                k = 0.09
                while k < 0.2 and len(approx) > 4:
                    approx = cv2.approxPolyDP(c, k * peri, True)
                    approx = cv2.convexHull(approx)#find凸多邊形框
                    k = k + 0.03
                    if len(approx) == 4 :
                        cv2.drawContours(image, [approx], -1, (0,0,255), 3)
                        break
            elif len(approx) < 4:
                k = 0.07
                while k > 0.02 and len(approx) < 4:
                    approx = cv2.approxPolyDP(c, k * peri, True)
                    approx = cv2.convexHull(approx)#find凸多邊形框
                    k = k - 0.02
                    if len(approx) == 4 :
                        cv2.drawContours(image, [approx], -1, (0,0,255), 3)
                        break

        if len(approx) == 4 :
            ret = True
            break
    
    if(len(approx)):
        M = cv2.moments(approx)
        if (M['m00'] != 0):
            cx = int(M['m10']/M['m00'])
            cy = int(M['m01']/M['m00'])
            cv2.circle(image, (cx,cy), 3, (0, 255, 255), -1)
        if cy >= img.shape[0] or cx >= img.shape[1] or cx < 0 or cy < 0:
            cx,cy = 0,0
        
    if len(approx) >= 4 :
        ret = True
    
    return ret, image, approx, cx, cy


# In[3]:


def find_quad_mask(new_approx, shape):
    #get mask of quadrilateral 
    height = shape[0]
    width = shape[1]
    mask = np.ones((height,width))
    polygon = []
    
    if (len(new_approx) >= 4):
        polygon=[(new_approx[0,0,1],new_approx[0,0,0]), (new_approx[1,0,1],new_approx[1,0,0]), (new_approx[2,0,1],new_approx[2,0,0]), (new_approx[3,0,1],new_approx[3,0,0])]
    elif (len(new_approx) == 3):
        polygon=[(new_approx[0,0,1],new_approx[0,0,0]), (new_approx[1,0,1],new_approx[1,0,0]), (new_approx[2,0,1],new_approx[2,0,0])]

    if (len(new_approx) >= 3 ):
        successful = True
        poly_path=Path(polygon)

        x, y = np.mgrid[:height, :width]
        coors=np.hstack((x.reshape(-1, 1), y.reshape(-1,1))) # coors.shape is (4000000,2)

        mask = poly_path.contains_points(coors)
        mask = mask.reshape(height, width)
    else:
        successful = False
    return successful, mask


# In[4]:


# def find_quad(img):
#     find_quad_success, find_quad_img, approx, cx, cy = find_quadrilateral(img)
    
#     if find_quad_success:
#         mask_success, mask = find_quad_mask(approx, image.shape)
#     else:
#         mask_success = False
        
#     return mask_success, find_quad_img, approx, cx, cy, mask


# In[5]:


# get_ipython().system('jupyter nbconvert --to script _Find_quad.ipynb')


# In[ ]:




