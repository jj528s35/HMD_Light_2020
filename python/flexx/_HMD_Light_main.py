#!/usr/bin/env python
# coding: utf-8

# In[1]:


import argparse
import roypy
import time
import queue
from sample_camera_info import print_camera_info
from roypy_sample_utils import CameraOpener, add_camera_opener_options
#from roypy_platform_utils import PlatformHelper

import numpy as np
import matplotlib.pyplot as plt
import cv2
import random
import math
from matplotlib.path import Path

import socket_sender
import _RANSAC
import _Find_quad
import _HMD_Light_function
import _Tranform_Data


try:
    import roypycy
except ImportError:
    print("Pico Flexx backend requirements (roypycy) not installed properly")
    raise


# In[2]:


def depth_range_mask(depthImg, low, height):
    highter_region = depthImg > low
    lower_region = depthImg < height
    depth_mask = np.logical_and(highter_region, lower_region)
    
    depth_mask_ = depth_mask.astype(np.uint8)*255
    
    kernel_size = 5 #7
    kernel = cv2.getStructuringElement(cv2.MORPH_RECT,(kernel_size, kernel_size))
    #膨胀之后再腐蚀，在用来关闭前景对象里的小洞或小黑点
    #开运算用于移除由图像噪音形成的斑点
    opened = cv2.morphologyEx(depth_mask_, cv2.MORPH_OPEN, kernel)
    depth_mask_ = cv2.morphologyEx(opened,cv2.MORPH_CLOSE,kernel)
    
     #find Contours with largest area 
    (_, cnts, _) = cv2.findContours(depth_mask_, cv2.RETR_LIST, cv2.CHAIN_APPROX_SIMPLE)
    if len(cnts) > 0:
        cnt_ = sorted(cnts, key = cv2.contourArea, reverse = True)[0]
        
        peri = cv2.arcLength(cnt_, True)
        approx = cv2.approxPolyDP(cnt_, 0.03* peri, True)
        hull = cv2.convexHull(approx)
        
        #mask of max contour area
        max_area_mask = np.zeros(depthImg.shape, dtype='uint8')  #依Contours圖形建立mask
        cv2.drawContours(max_area_mask, [hull], -1, 255, -1) #255        →白色, -1→塗滿
        
        #mask the depth mask with max_area_mask
        mask = cv2.add(depth_mask_, np.zeros(np.shape(depthImg), dtype='uint8'), mask=max_area_mask)
        
        depth_img_with_mask = cv2.add(depthImg, np.zeros(np.shape(depthImg), dtype=np.float32), mask=mask)
    
    else:
        depth_img_with_mask = cv2.add(depthImg, np.zeros(np.shape(depthImg), dtype=np.float32), mask=depth_mask_)
        mask = depth_mask_
    
    
    
    return mask, depth_img_with_mask


# In[3]:


def get_edge_map(grayImage,depthImage):
#     """
#     Canny Edge map
#     turn grayImg from int32 to int8
#     blur the grayImg then do Canny Edge
#     """
#     low_threshold = 2
#     high_threshold = 10
    
#     kernel_size = 3
#     blur_gray = cv2.GaussianBlur(grayImage,(kernel_size, kernel_size), 0)
#     Cannyedges = cv2.Canny(grayImage, low_threshold, high_threshold)#blur_gray
    
    """
    Threshold based Edge map
    if depth between the pixel and its nearby pixels > near_depth_threshold, then labeled it
    """
    s_time = time.time()
    near_depth_threshold = 0.1 #10cm
#     print(np.max(depthImage))
    Threshold_based_edge = np.zeros((depthImage.shape[0],depthImage.shape[1]))
    
    h = depthImage.shape[0]
    w = depthImage.shape[1]
    depth_img_transform = np.zeros((h+1,w+1))
    depth_img_transform[:h,:w] = depthImage
    #check left up depth threshold
    depth_img_transform[1:h+1,1:w+1] = depthImage
    check_depth_threshold = abs(depthImage - depth_img_transform[:h,:w]) > near_depth_threshold
    Threshold_based_edge = np.logical_or(Threshold_based_edge, check_depth_threshold)
    #check up depth threshold
    depth_img_transform[:h,:w] = depthImage
    depth_img_transform[1:h+1,:w] = depthImage
    check_depth_threshold = abs(depthImage - depth_img_transform[:depthImage.shape[0],:depthImage.shape[1]]) > near_depth_threshold
    Threshold_based_edge = np.logical_or(Threshold_based_edge, check_depth_threshold)
    #check Right up depth threshold
    depth_img_transform[:h,:w] = depthImage
    depth_img_transform[1:h+1,:w-1] = depthImage[:,1:w]
    check_depth_threshold = abs(depthImage - depth_img_transform[:h,:w]) > near_depth_threshold
    Threshold_based_edge = np.logical_or(Threshold_based_edge, check_depth_threshold)
    #check Left depth threshold
    depth_img_transform[:h,:w] = depthImage
    depth_img_transform[:h,1:w+1] = depthImage
    check_depth_threshold = abs(depthImage - depth_img_transform[:h,:w]) > near_depth_threshold
    Threshold_based_edge = np.logical_or(Threshold_based_edge, check_depth_threshold)
    #check Right depth threshold
    depth_img_transform[:h,:w] = depthImage
    depth_img_transform[:h,:w-1] = depthImage[:,1:w]
    check_depth_threshold = abs(depthImage - depth_img_transform[:h,:w]) > near_depth_threshold
    Threshold_based_edge = np.logical_or(Threshold_based_edge, check_depth_threshold)
    #check Left down depth threshold
    depth_img_transform[:h,:w] = depthImage
    depth_img_transform[:h-1,1:w+1] = depthImage[1:h,:]
    check_depth_threshold = abs(depthImage - depth_img_transform[:h,:w]) > near_depth_threshold
    Threshold_based_edge = np.logical_or(Threshold_based_edge, check_depth_threshold)
    #check down depth threshold
    depth_img_transform[:h,:w] = depthImage
    depth_img_transform[:h-1,:w] = depthImage[1:h,:]
    check_depth_threshold = abs(depthImage - depth_img_transform[:h,:w]) > near_depth_threshold
    Threshold_based_edge = np.logical_or(Threshold_based_edge, check_depth_threshold)
    #check Right down depth threshold
    depth_img_transform[:h,:w] = depthImage
    depth_img_transform[:h-1,:w-1] = depthImage[1:h,1:w]
    check_depth_threshold = abs(depthImage - depth_img_transform[:h,:w]) > near_depth_threshold
    Threshold_based_edge = np.logical_or(Threshold_based_edge, check_depth_threshold)
    
    
#     print('*get threshold edge: %.4f s'%(time.time()-s_time))
    """
    Merge Canny Edge map and Threshold based Edge map
    """
#     Edge_map = np.logical_or(Cannyedges,Threshold_based_edge)
    
    return Threshold_based_edge
#     return Cannyedges,Threshold_based_edge, Edge_map


# In[4]:


class MyListener(roypy.IDepthDataListener):
    def __init__(self, xqueue, yqueue, zqueue, grayValuequeue):
        super(MyListener, self).__init__()
        self.xqueue = xqueue
        self.yqueue = yqueue
        self.zqueue = zqueue
        self.grayValuequeue = grayValuequeue
        self.Listening = True

    def onNewData(self, data):   
        if(self.Listening):
            t_time = time.time()
            
            xvalues = []
            yvalues = []
            zvalues = []
            grayvalues = []
            
            values = roypycy.get_backend_data(data)

            xvalues = values.x
            yvalues = values.y
            zvalues = values.z
            grayvalues = values.grayValue

            xarray = np.asarray(xvalues)
            yarray = np.asarray(yvalues)
            zarray = np.asarray(zvalues)
            
            
            q_x = xarray.reshape (-1, data.width)        
            self.xqueue.put(q_x)
            q_y = yarray.reshape (-1, data.width)        
            self.yqueue.put(q_y)
            q_z = zarray.reshape (-1, data.width)        
            self.zqueue.put(q_z)
            
            q_grayvalues = grayvalues.reshape (-1, data.width)        
            self.grayValuequeue.put(q_grayvalues)
            
            #print('store time:', (time.time()-t_time))

    def paint (self, data, name):
        """Called in the main thread, with data containing one of the items that was added to the
        queue in onNewData.
        """
        cv2.namedWindow(name, cv2.WINDOW_NORMAL)
        cv2.imshow(name, data)
        cv2.waitKey(1)


# In[5]:



def main ():
    parser = argparse.ArgumentParser (usage = __doc__)
    add_camera_opener_options (parser)
    parser.add_argument ("--seconds", type=int, default=15, help="duration to capture data")
#     parser.add_argument ("--SendData", type=bool, default=False, help="SendData")
#     parser.add_argument ("--Project_on_body", type=bool, default=False, help="Project_on_body")
#     parser.add_argument ("--Replay", type=bool, default=False, help="Replay")
    
    timer_show = False
    
    # 測試用 setting
    _Replay = True
    if(_Replay == True):
        options = parser.parse_args(args=['--rrf', 'upper_line_body.rrf','--seconds', '25'])#0211_forward
    else:
        options = parser.parse_args(args=['--seconds', '30'])
        
#     _SendData = False
#     if(_SendData == True):
#         options = parser.parse_args(args=['--SendData', 'False'])
#     else:
#         options = parser.parse_args(args=['--SendData', 'True'])
        
#     _Project_on_body = False
#     if(_Project_on_body == True):
#         options = parser.parse_args(args=['--Project_on_body', 'False'])
#     else:
#         options = parser.parse_args(args=['--Project_on_body', 'True'])
        

    opener = CameraOpener (options)
    cam = opener.open_camera ()
    
    if(_Replay == False):
        cam.setUseCase('MODE_5_45FPS_500')#MODE_9_5FPS_2000 MODE_5_45FPS_500 MODE_5_35FPS_600

    #Print camera information
    print_camera_info (cam)
    print("isConnected", cam.isConnected())
    print("getFrameRate", cam.getFrameRate())
    print("UseCase",cam.getCurrentUseCase())

    # we will use this queue to synchronize the callback with the main
    # thread, as drawing should happen in the main thread 
    x = queue.LifoQueue()
    y = queue.LifoQueue()
    z = queue.LifoQueue()
    grayvalue = queue.LifoQueue()
    l = MyListener(x,y,z,grayvalue)
    cam.registerDataListener(l)
    cam.startCapture()
    
    # create a loop that will run for a time (default 15 seconds)
    process_event_queue (x, y, z, grayvalue, l, options.seconds, cam)
    cam.stopCapture()
    socket_sender.close_socket()
    
    cv2.destroyAllWindows()
    


# In[6]:


def process_event_queue (x,y,z,grayvalue, painter, seconds, cam):
    SendData = False
    Project_on_body = True
    
    #initial
    last_inliner_num = 20000
    quad_mask = np.zeros((171,224))
    circles_image = np.zeros((171,224,3))
    
    # create a loop that will run for the given amount of time
    print("  Quit : Q\n")
    while 1 :
        t_time = time.time()
        try:
            # try to retrieve an item from the queue.
            # this will block until an item can be retrieved
            # or the timeout of 1 second is hit
            t_time = time.time()
            
            item_x = x.get(True, 0.5)
            item_y = y.get(True, 0.5)
            item_z = z.get(True, 0.5)
            points3D = np.dstack((item_x,item_y,item_z))
            item_grayvalue = grayvalue.get(True, 0.5)
            #print('queue time:', (time.time()-t_time))
        except queue.Empty:
            # this will be thrown when the timeout is hit
            print("Empty")
#             break
            continue
        else:
            #turn item_grayvalue to uint8
            grayvalue_img = cv2.convertScaleAbs(item_grayvalue)
            
            #if there have z value(z != 0) ==> True
            Confidence_img = points3D[:,:,2] != 0 
            
            if Project_on_body:
                # mask the depth image with depth range
                depth_mask, depth_img_with_mask = depth_range_mask(item_z, 0.25, 0.6)#0.3m(30cm) 0.5m(50cm)

                #turn bool img to uint8
                depth_mask = depth_mask.astype(np.uint8)*255  
                
#                 #find single targetline
#                 find_target_success, circles_image, px, py, minX, minY, maxX, maxY = \
#                                 _HMD_Light_function.find_targetLine_pos(grayvalue_img, depth_mask, Confidence_img)
                
                
                #find target plane
                circles_image, plane_x, plane_y = _HMD_Light_function.find_target_plane(grayvalue_img, depth_mask, Confidence_img, points3D)

                Threshold_based_edge = get_edge_map(grayvalue_img, depth_img_with_mask)
            
            
                _HMD_Light_function.show_plane(plane_x, plane_y, points3D)
            
                if(SendData ):#and find_target_success
                    _Tranform_Data.send_target_plane(plane_x, plane_y, points3D)
#                     for single targetline
#                     _Tranform_Data.send_targetpos(px, py, points3D)
#                     _Tranform_Data.send_forward_vector(minX, minY, maxX, maxY, points3D)
                
            
                #show image
#                 painter.paint (item_z, 'Depth')
#                 painter.paint (depth_img_with_mask, 'depth_img_with_mask')
#                 painter.paint (grayvalue_img, 'grayvalue_img')
                painter.paint (circles_image, 'circles_image')

    #             painter.paint (depth_img_with_mask, 'depth_img_with_mask')
#                 painter.paint (Threshold_based_edge.astype(np.uint8)*255, 'Threshold_based_edge')
#                 painter.paint (Cannyedges.astype(np.uint8)*255, 'Cannyedges')

            
    
                #Send data to unity
#                 receive data from unity
                if(SendData):
                   
                    stop = _Tranform_Data.receive_data(cam)
                    if stop:
                        break
    
            else: #Project_on_Floor
                #find plane by RANSAC
                surface_plane, depthImg, plane_mask, best_sampts, best_inlinernum =                 _RANSAC.RANSAM(points3D, Confidence_img, ransac_iteration = 50, inliner_threshold = 0.01, last_inliner_num = last_inliner_num)#1cm  0.003
                last_inliner_num = best_inlinernum
                
                #turn bool img to uint8
                plane_img = plane_mask.astype(np.uint8)*255 
                
                #find large 四邊形 on the mask of plane, and find the center of it
                find_quad_success, find_quad_img, approx, cx, cy = _Find_quad.find_quadrilateral(plane_img)
                
                if find_quad_success:
                    # Get the quad_mask
                    mask_success, quad_mask = _Find_quad.find_quad_mask(approx, plane_img.shape)
                    if mask_success:
                        #find target circle
#                         find_target_success, px, py, circles_image = _HMD_Light_function.find_target_circle(grayvalue_img, quad_mask, plane_mask)
                        
                        #find single targetline
                        find_target_success, circles_image, px, py, minX, minY, maxX, maxY =                                 _HMD_Light_function.find_targetLine_pos(grayvalue_img, quad_mask, plane_mask)
                        
                        #find line => not good result
#                         line_image = _HMD_Light_function.find_line(grayvalue_img, quad_mask)
            
                        if(SendData and find_target_success):
                            _Tranform_Data.send_targetpos(px, py, points3D)
                            
#                             vx, vy = _HMD_Light_function.find_plane_forward_vector(px, approx, plane_mask, 30)
#                             _Tranform_Data.send_forward_vector(px, py, vx, vy, points3D)

                            _Tranform_Data.send_forward_vector(minX, minY, maxX, maxY, points3D)
                            
                            # 2個send中要有延遲(show image 或 print)才不會卡
#                             print("1") 
                            
        
                #show image
                painter.paint (item_z, 'Depth')
#                 painter.paint (line_image, 'line_image')
                painter.paint (grayvalue_img, 'grayvalue_img')
#                 painter.paint (plane_img, 'plane_mask')
                painter.paint (circles_image, 'circles_image')
                
                
                if(SendData):
                    _Tranform_Data.send_plane_eq(surface_plane)
                    
                    stop = _Tranform_Data.receive_data(cam)
                    if stop:
                        break
                        
                
        
#             print('time:', (time.time()-t_time))
        
        
                
        if(cv2.waitKey(10) & 0xFF == 113):#耗時0.01s
            break


# In[7]:


main()


# In[8]:


socket_sender.close_socket()


# In[ ]:





# In[9]:


# !jupyter nbconvert --to script _HMD_Light_main.ipynb


# In[ ]:





# In[ ]:





# In[ ]:




