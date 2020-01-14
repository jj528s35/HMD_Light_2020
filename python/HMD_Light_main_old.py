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
import socket_sender

try:
    import roypycy
except ImportError:
    print("Pico Flexx backend requirements (roypycy) not installed properly")
    raise


# # RANSAM

# In[7]:


# # RANSAM

def Dis_pt2plane(pts, a, b, c, d):
    """
    Compute the distance from points to the plane
    """
    normal = math.sqrt(a*a+b*b+c*c)
    if normal == 0:
        normal = 1
    
    v = np.array([a,b,c])
    dis = abs(np.dot(pts,v.T)+d)/normal
    return dis

def get_Plane(sampts):
    """
    Compute the equation of the plane
    """
    p1 = sampts[0]
    p2 = sampts[1]
    p3 = sampts[2]
    
    a = ( (p2[1]-p1[1])*(p3[2]-p1[2])-(p2[2]-p1[2])*(p3[1]-p1[1]) )
    b = ( (p2[2]-p1[2])*(p3[0]-p1[0])-(p2[0]-p1[0])*(p3[2]-p1[2]) )
    c = ( (p2[0]-p1[0])*(p3[1]-p1[1])-(p2[1]-p1[1])*(p3[0]-p1[0]) )
    d = ( 0-(a*p1[0]+b*p1[1]+c*p1[2]) )
    
    return a,b,c,d

# def Random3points(points3D, ConfidenceIndex):
#     """
#     Random choose 3 Confidence points
#     """
#     sample_number = 3
#     sample_point_index = random.sample(range(ConfidenceIndex.shape[0]), sample_number)
#     sample_points = np.zeros((sample_number,3))
#     for i in range(sample_number):
#         Confidence_point_index = sample_point_index[i]
#         index = ConfidenceIndex[Confidence_point_index]
#         y = index // points3D.shape[1]
#         x = index % points3D.shape[1]
#         sample_points[i] = points3D[y][x]
#     return sample_points

def Random3points(points3D):
    sample_number = 20
    sample_point_index = random.sample(range(points3D.shape[0]*points3D.shape[1]), sample_number)
    sample_points = np.zeros((3,3))
    num = 0
    for i in range(sample_number):
        index = sample_point_index[i]
        y = index // points3D.shape[1]
        x = index % points3D.shape[1]
        
        point = points3D[y][x]
        if(point[0] != 0 or point[1] != 0 or point[2] != 0):
            sample_points[num] = points3D[y][x]
            num = num + 1
        
        if(num == 3):
            break
    return sample_points

def get_inliner_num(points3D,a,b,c,d,inliner_threshold):
    """
    Compute the liner points which distance to plane < threshold
    Also get distance from points to the plane (new Depth Image which re-project depth pixels in surface plane)
    """
    inliner_num = 0
    
    dist = Dis_pt2plane(points3D,a,b,c,d)
    inliner_mask = dist < inliner_threshold
    inliner_num = np.sum(inliner_mask)
    return inliner_num, inliner_mask, dist

def RANSAM(points3D, ransac_iteration = 1000, inliner_threshold = 0.01):
    best_inlinernum = -1
    best_inlinernum = 0
    best_plane = np.zeros((1,4))
    best_depthImage = np.zeros((points3D.shape[0],points3D.shape[1]))
    best_plane_mask = np.zeros((points3D.shape[0],points3D.shape[1]))
    best_sampts = np.zeros((3,3))
    
#     print(points3D.shape,points3D[80:90,110])
    for i in range(ransac_iteration):
        sampts = Random3points(points3D)
        a,b,c,d = get_Plane(sampts)
        
        inliner_num, inliner_mask, depthImage = get_inliner_num(points3D,a,b,c,d,inliner_threshold)
        if(inliner_num > best_inlinernum):
            best_inlinernum = inliner_num
            best_plane = np.array([a,b,c,d])
            best_plane_mask = inliner_mask
            best_depthImage = depthImage
            best_sampts = sampts
            
    print("Inliner Number\n", best_inlinernum)
    print("Inliner plane\n", best_plane)
    return best_plane, best_depthImage, best_plane_mask, best_sampts


# # Draw 3D plane

# In[8]:


import numpy as np
import matplotlib.pyplot as plt
from mpl_toolkits.mplot3d import Axes3D

def show_plane(plane_eq):
    a,b,c,d = plane_eq[0], plane_eq[1], plane_eq[2], plane_eq[3]
    x = np.linspace(-1,1,10)
    y = np.linspace(-1,1,10)

    X,Y = np.meshgrid(x,y)
    Z = (d - a*X - b*Y) / c

    fig = plt.figure()
    ax = fig.gca(projection='3d')

    surf = ax.plot_surface(X, Y, Z)


# # Tranform Data

# In[9]:


def send_plane_eq(plane_eq):
    data = ""
    temp = ['1 ']
    temp.append("%lf %lf %lf %lf"%(plane_eq[0], plane_eq[1], plane_eq[2], plane_eq[3]))
    data = data.join(temp)
    print('send plane_eq:', data)
    socket_sender.send(data)
    
def send_sample_points(sample_points):
    data = ""
    temp = ['2 ']
    for i in range(3): 
        temp.append("%f %f %f "%(sample_points[i][0], sample_points[i][1], sample_points[i][2]))
    data = data.join(temp)
    print('send sample point:', data)
    socket_sender.send(data)


# # Main

# In[13]:




class MyListener(roypy.IDepthDataListener):
    def __init__(self, xqueue, yqueue, zqueue):
        super(MyListener, self).__init__()
        self.xqueue = xqueue
        self.yqueue = yqueue
        self.zqueue = zqueue
        self.Listening = True

    def onNewData(self, data):   
        if(self.Listening):
            t_time = time.time()
            
            xvalues = []
            yvalues = []
            zvalues = []
            
            values = roypycy.get_backend_data(data)

            xvalues = values.x
            yvalues = values.y
            zvalues = values.z

            xarray = np.asarray(xvalues)
            yarray = np.asarray(yvalues)
            zarray = np.asarray(zvalues)
            
            
            q_x = xarray.reshape (-1, data.width)        
            self.xqueue.put(q_x)
            q_y = yarray.reshape (-1, data.width)        
            self.yqueue.put(q_y)
            q_z = zarray.reshape (-1, data.width)        
            self.zqueue.put(q_z)
            
            #print('store time:', (time.time()-t_time))

    def paint (self, data, name):
        """Called in the main thread, with data containing one of the items that was added to the
        queue in onNewData.
        """
        cv2.namedWindow(name, cv2.WINDOW_NORMAL)
        cv2.imshow(name, data)
        cv2.waitKey(1)


def main ():
    parser = argparse.ArgumentParser (usage = __doc__)
    add_camera_opener_options (parser)
    parser.add_argument ("--seconds", type=int, default=15, help="duration to capture data")
    timer_show = False
    
    Replay = False
    if(Replay == True):
        options = parser.parse_args(args=['--rrf', '0108.rrf','--seconds', '5'])
    else:
        options = parser.parse_args(args=['--seconds', '10'])

    opener = CameraOpener (options)
    cam = opener.open_camera ()
    
    if(Replay == False):
        cam.setUseCase('MODE_5_35FPS_600')#MODE_9_5FPS_2000 MODE_5_45FPS_500

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
    l = MyListener(x,y,z)
    cam.registerDataListener(l)
    cam.startCapture()
    
    # create a loop that will run for a time (default 15 seconds)
    process_event_queue (x, y, z, l, options.seconds)
    cam.stopCapture()
    
    cv2.destroyAllWindows()
    

def process_event_queue (x,y,z, painter, seconds):
    show_3d_plane_img = False 
    
    # create a loop that will run for the given amount of time
#     t_end = time.time() + seconds
#     while time.time() < t_end:
    key = ''
    print("  Quit : Q\n")
    while key != 113:
        try:
            # try to retrieve an item from the queue.
            # this will block until an item can be retrieved
            # or the timeout of 1 second is hit
            t_time = time.time()
            
            item_x = x.get(True, 0.5)
            item_y = y.get(True, 0.5)
            item_z = z.get(True, 0.5)
            points3D = np.dstack((item_x,item_y,item_z))
            #print('queue time:', (time.time()-t_time))
        except queue.Empty:
            # this will be thrown when the timeout is hit
            break
        else:
            painter.paint (item_z, 'Depth')
            t_time = time.time()
#             surface_plane, depthImg, plane_mask, best_sampts = RANSAM(points3D, ConfidenceIndex, ransac_iteration = 500, inliner_threshold = 0.003)
            surface_plane, depthImg, plane_mask, best_sampts = RANSAM(points3D, ransac_iteration = 50, inliner_threshold = 0.01)#1cm  0.003
            print('Ransam time:', (time.time()-t_time))
            painter.paint (plane_mask.astype(np.uint8)*255, 'plane')
            
            #Send surface_plane and best_sampts
            send_plane_eq(surface_plane)
            send_sample_points(best_sampts)
            
            if(show_3d_plane_img):
                show_plane(surface_plane)
                
        key = cv2.waitKey(1)


# In[16]:




main()


# In[17]:


# get_ipython().system('jupyter nbconvert --to script HMD_Light_main.ipynb')

