#!/usr/bin/env python
# coding: utf-8

# In[1]:


import numpy as np
import matplotlib.pyplot as plt
import cv2


# In[2]:

#20200215 find target circle
def find_targetBlob_pos(img, quad_mask):
    ret = False
    px = 0
    py = 0
    mask = quad_mask.astype(np.uint8)*255
    circles_image = cv2.add(img, np.zeros(np.shape(img), dtype=np.uint8), mask=mask)
    
#     ret,circles_image = cv2.threshold(circles_image,220,255,cv2.THRESH_BINARY)
    
    params = cv2.SimpleBlobDetector_Params()
    # Filter by Area.
    params.filterByArea = True
    params.minArea = 50

    # Filter by Circularity
    params.filterByCircularity = True
    params.minCircularity = 0.1

    # Filter by Convexity
    params.filterByConvexity = True
    params.minConvexity = 0.87

    # Filter by Inertia
    params.filterByInertia = True
    params.minInertiaRatio = 0.01
    
    # Set up the detector with default parameters.
    detector = cv2.SimpleBlobDetector_create(params)

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


def find_target_circle(grayvalue_img, quad_mask, plane_mask):
    #find circle
    success, px, py, circles_image = find_targetBlob_pos(grayvalue_img, quad_mask)
    if success:
        targetpos = (px, py)
        if(plane_mask[py,px] == False):
            px, py = find_points_on_plane(py, px, plane_mask, kernel_size = 7)
    return success, px, py, circles_image
#20200215 find target circle


#20200216 find single target line
def find_targetLine_pos(img, quad_mask, plane_mask):
    ret = False
    px = 0
    py = 0
    mask = quad_mask.astype(np.uint8)*255
    circles_image = cv2.add(img, np.zeros(np.shape(img), dtype=np.uint8), mask=mask)
    x = np.zeros((2,1))
    y = np.zeros((2,1))
    point_list = []
    
#     ret,circles_image = cv2.threshold(circles_image,220,255,cv2.THRESH_BINARY)
    
    params = cv2.SimpleBlobDetector_Params()
    # Filter by Area.
    params.filterByArea = True
    params.minArea = 30#50
    params.maxArea = 60

    # Filter by Circularity
    params.filterByCircularity = True
    params.minCircularity = 0.1

    # Filter by Convexity
    params.filterByConvexity = True
    params.minConvexity = 0.87

    # Filter by Inertia
    params.filterByInertia = True
    params.minInertiaRatio = 0.01
    
    # Set up the detector with default parameters.
    detector = cv2.SimpleBlobDetector_create(params)

    # Detect blobs.
    keypoints = detector.detect(circles_image)

    centerX = 0
    centerY = 0 
    minX = 0 
    minY = 0 
    maxX = 0 
    maxY = 0
    if len(keypoints) > 0:
        ret = True
        circles_image = cv2.cvtColor(circles_image, cv2.COLOR_GRAY2BGR)
        circles_image = cv2.drawKeypoints(circles_image, keypoints, np.array([]), (0,255,0), cv2.DRAW_MATCHES_FLAGS_DRAW_RICH_KEYPOINTS)
        
        x = np.zeros((len(keypoints),1))
        y = np.zeros((len(keypoints),1))
        for i in range(len(keypoints)):
            px = int(keypoints[i].pt[0])
            py = int(keypoints[i].pt[1])
            x[i] = px
            y[i] = py
#             cv2.circle(circles_image, (px,py), 2, (0,0,225), 2)

        #find the center, leftmost, rightmost points in these points
        centerX,centerY,minX,minY,maxX,maxY = find_line_center_and_vector(x,y)
        centerX,centerY,minX,minY,maxX,maxY = find_center_and_vector_with_depth_value(plane_mask,centerX,centerY,minX,minY,maxX,maxY)
        
#         print(centerX,centerY,minX,minY,maxX,maxY)
        cv2.circle(circles_image, (centerX,centerY), 2, (0,225,225), 2)
        cv2.circle(circles_image, (maxX,maxY), 2, (225,0,0), 2)
        cv2.circle(circles_image, (minX,minY), 2, (225,0,0), 2)
        
    return ret, circles_image, centerX, centerY, minX, minY, maxX, maxY


#將線性回歸的函式庫載入，準備要執行線性回歸
from sklearn.linear_model import LinearRegression
def find_line_center_and_vector(x,y):
    #首先開一台線性回歸機
    regr = LinearRegression()
    
    #透過LinearRegression.fit()去進行機器學習
    #參數餵給他修正過後的X以及正確答案y
    regr.fit(x,y)

    #取出機器學習的結果LinearRegression.predict
    #注意這裡傳入的參數是修正過的X
    Y = regr.predict(x)
    
    
    centerX = int(np.mean(x))
    centerY = int(np.mean(Y))
    
    minY = 171
    minX = 224
    maxY = 0
    maxX = 0
    for i in range(x.shape[0]):
        if x[i] < minX:
            minY = int(Y[i,0])
            minX = int(x[i,0])
            
        if x[i] > maxX:
            maxY = int(Y[i,0])
            maxX = int(x[i,0])
            
    return centerX,centerY,minX,minY,maxX,maxY

def find_center_and_vector_with_depth_value(plane_mask,centerX,centerY,minX,minY,maxX,maxY):
    if(plane_mask[centerY, centerX] == False):
            centerX, centerY = find_points_on_plane(centerY, centerX, plane_mask, kernel_size = 7)
            
    if(plane_mask[minY, minX] == False):
            minX, minY = find_points_on_plane(minY, minX, plane_mask, kernel_size = 7)
            
    if(plane_mask[maxY, maxX] == False):
            maxX, maxY = find_points_on_plane(maxY, maxX, plane_mask, kernel_size = 7)
    
    return centerX, centerY, minX, minY, maxX, maxY
#20200216 find single target line


#20200217 find target plane
def find_targetLine(img, quad_mask, plane_mask):
    ret = False
    px = 0
    py = 0
    mask = quad_mask.astype(np.uint8)*255
    circles_image = cv2.add(img, np.zeros(np.shape(img), dtype=np.uint8), mask=mask)
    x = np.zeros((2,1))
    y = np.zeros((2,1))
    point_list = []
    
#     ret,circles_image = cv2.threshold(circles_image,220,255,cv2.THRESH_BINARY)
    
    params = cv2.SimpleBlobDetector_Params()
    # Filter by Area.
    params.filterByArea = True
    params.minArea = 30#50
    params.maxArea = 70

    # Filter by Circularity
    params.filterByCircularity = True
    params.minCircularity = 0.1

    # Filter by Convexity
    params.filterByConvexity = True
    params.minConvexity = 0.87

    # Filter by Inertia
    params.filterByInertia = True
    params.minInertiaRatio = 0.01
    
    # Set up the detector with default parameters.
    detector = cv2.SimpleBlobDetector_create(params)

    # Detect blobs.
    keypoints = detector.detect(circles_image)

    centerX = 0
    centerY = 0 
    minX = 0 
    minY = 0 
    maxX = 0 
    maxY = 0
    if len(keypoints) > 0:
        ret = True
        circles_image = cv2.cvtColor(circles_image, cv2.COLOR_GRAY2BGR)
        circles_image = cv2.drawKeypoints(circles_image, keypoints, np.array([]), (0,255,0), cv2.DRAW_MATCHES_FLAGS_DRAW_RICH_KEYPOINTS)
        
#         keypoints = sorted(keypoints, key = keypoints.pt[0], reverse = True)
        keypoints.sort(key=(lambda s: s.pt[0]))
        
        x = np.zeros((len(keypoints)), dtype=int)
        y = np.zeros((len(keypoints)), dtype=int)
        for i in range(len(keypoints)):
            px = int(keypoints[i].pt[0])
            py = int(keypoints[i].pt[1])
            x[i] = px
            y[i] = py
            cv2.circle(circles_image, (px,py), 1, (0,0,225), 1)
        
    return ret, circles_image, x, y

# In[3]:
def find_line_slope(x,y):
    #首先開一台線性回歸機
    regr = LinearRegression()
    
    X = x.reshape((-1, 1))
    
    #透過LinearRegression.fit()去進行機器學習
    #參數餵給他修正過後的X以及正確答案y
    regr.fit(X,y)
    
    slope = regr.coef_
    
    return slope[0], x, y

def find_new_target_line_in_3d(circles_image, x, y, p, V_slope, points_3d, plane_mask):
    success = True
    slope_dist = np.sqrt(1 + V_slope*V_slope)
    #dist between a and a' is 5
    dist = 10 # 10 pixel
    t = dist/slope_dist

    # point a' position in pixel coord
    new_x = np.zeros((len(x)), dtype=int)
    new_y = np.zeros((len(y)), dtype=int)
    for i in range(x.shape[0]):
        if(V_slope > 0):
            new_x[i] = int(x[i] - round(1 * t))
            new_y[i] = int(y[i] - round(V_slope * t))
        else:
            new_x[i] = int(x[i] + round(1 * t))
            new_y[i] = int(y[i] + round(V_slope * t))
#         circles_image = cv2.circle(circles_image, (new_x[i],new_y[i]), 1, (225,0,0), -1)
        
    real_dist = 0.03 # 3 cm
    #intrinsic matrix
    fx = 211.787
    fy = 211.6044
    cx = 117.6575
    cy = 87.0219
    v = np.zeros((3),dtype=float)
    new_p = np.zeros((len(x),3),dtype=float) #new 3d point which 
    #new line which dist between origional one is 5 cm
    new_u = np.zeros((len(x)), dtype=int)
    new_v = np.zeros((len(y)), dtype=int)
    for i in range(x.shape[0]):
#         _x = x[i]
#         _y = y[i]
        _nx = new_x[i] 
        _ny = new_y[i] 
        
        
#         #boundary
#         if _x >= circles_image.shape[1]:
#             _x = circles_image.shape[1]-1
            
#         if _y >= circles_image.shape[0]:
#             _y = circles_image.shape[0]-1
        
        #boundary
        if _nx >= circles_image.shape[1]:
            _nx = circles_image.shape[1]-1
            success = False
            
        if _ny >= circles_image.shape[0]:
            _ny = circles_image.shape[0]-1
            success = False
            
#         #check if it have value
#         if(plane_mask[_y,_x] == False):
#             _x, _y = find_points_on_plane(_y,_x, plane_mask, kernel_size = 7)
            
        if(plane_mask[_ny,_nx] == False):
            _nx, _ny = find_points_on_plane(_ny,_nx, plane_mask, kernel_size = 3)
            if(plane_mask[_ny,_nx] == False):
                success = False
        
        # vector from a to a' in 3d coord
        v[0] = points_3d[_ny,_nx,0]-p[i,0] #points_3d[_y,_x,0]
        v[1] = points_3d[_ny,_nx,1]-p[i,1] #points_3d[_y,_x,1]
        v[2] = points_3d[_ny,_nx,2]-p[i,2] #points_3d[_y,_x,2]
        #dist between a to a' in 3d coord
        v_dist = np.sqrt(v[0]*v[0]+v[1]*v[1]+v[2]*v[2])
        
        if v_dist == 0:
            success = False
            return success, circles_image, new_u, new_v, new_p
        
        # point p which is 5 cm from the point a with vector v
        _t = real_dist/v_dist
        new_p[i,0] = p[i,0] + v[0] * _t #points_3d[_y,_x,0] + v[0] * _t
        new_p[i,1] = p[i,1] + v[1] * _t #points_3d[_y,_x,1] + v[1] * _t
        new_p[i,2] = p[i,2] + v[2] * _t #points_3d[_y,_x,2] + v[2] * _t
        
        # reproject point p to pixel coord
        # x' = X/Z   y' = Y/Z
        # u = fx*x'+cx
        if new_p[i,2] == 0:
            new_p[i,2] = 1
#             print(_x,_y)
            
        x_ = new_p[i,0]/ new_p[i,2]
        y_ = new_p[i,1]/ new_p[i,2]
        u_ = int(round(fx*x_ + cx))
        v_ = int(round(fy*y_ + cy))
        
        new_u[i] = u_
        new_v[i] = v_
        circles_image = cv2.circle(circles_image, (u_,v_), 2, (255,255,0), -1)
        
    return success, circles_image, new_u, new_v, new_p
    
def uv_to_3d(x,y,points_3d, plane_mask):
    success = True
    p = np.zeros((len(x),3))
    for i in range(len(x)):
        if (points_3d[y[i],x[i], 0] == False):
            x[i],y[i] = find_points_on_plane(y[i],x[i], plane_mask, 3)
            if (points_3d[y[i],x[i], 0] == False):
                success = False
        p[i,:] = points_3d[y[i],x[i],:]
    return success, p
    
    
def find_target_plane(img, quad_mask, plane_mask, points_3d, k = 4):
    success, circles_image, x, y = find_targetLine(img, quad_mask, plane_mask)
    k = 4 #num of line
    plane_x = np.zeros((k,len(x)), dtype=int)
    plane_y = np.zeros((k,len(x)), dtype=int)
    plane_points = np.zeros((k,len(x),3), dtype=float)
        
    if success:
        #if len > 5，去掉沒有深度資料的點(可能是dpeth mask的mask中沒有深度資料的部分被mask掉，使grayvalue中多出一塊mask出的黑色區塊=>多判斷出一個點)
        if len(x) > 5:
            _x = np.zeros((len(x)-1), dtype=int)
            _y = np.zeros((len(y)-1), dtype=int)
#             print(x,y)
            j = 0
            for i in range(len(x)):
                #boundary
                if x[i] >= circles_image.shape[1]:
                    x[i] = circles_image.shape[1]-1

                if y[i] >= circles_image.shape[0]:
                    y[i] = circles_image.shape[0]-1
                    
                if plane_mask[y[i], x[i]] == False:
                    continue
                else:
                    if j == k: 
                        break
                    _x[j] = x[j]
                    _y[j] = y[j]
                    j = j + 1
                    
#             print("new : ",_x,_y)
            x, y = _x,_y
           
        
#         slope, x, y = find_line_slope(x,y)
#         V_slope = -1/slope  
        
        
        plane_x = np.zeros((k,len(x)), dtype=int)
        plane_y = np.zeros((k,len(x)), dtype=int)
        plane_x[0,:] = x
        plane_y[0,:] = y
        
        plane_points = np.zeros((k,len(x),3), dtype=float)
        success, p = uv_to_3d(x, y, points_3d, plane_mask)
        plane_points[0,:] = p
        
        for i in range(1,k):
            slope, x, y = find_line_slope(x,y)
            if slope == 0:
                slope = 1
            V_slope = -1/slope  
            newline_success, circles_image, x, y, p = find_new_target_line_in_3d(circles_image, x, y, p, V_slope, points_3d, plane_mask)
            plane_x[i,:] = x
            plane_y[i,:] = y
            plane_points[i,:] = p
            
            if newline_success == False:
                success = False
                break

    return success, circles_image, plane_x, plane_y, plane_points
#20200217 find target plane

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




# In[5]:
# 20200214
# find_plane_forward_vector by circle and a points with offset y => Calibration (fake效果，方便用來對齊小黑點和投影位置)
#從circle(px,py)往y方向上方找點(px,y)
def find_plane_forward_vector(px, approx, plane_mask, step = 30):
    y = 0
    if len(approx) > 0:
        y = approx[0,0,1]
    for i in range(len(approx)):
        if approx[i,0,1] < y:
            y = approx[i,0,1]
    if(plane_mask[y,px] == False):
        px, y = find_points_on_plane(y, px, plane_mask, kernel_size = 7)
        
    return px, y
# 20200214

# def MorphologyEx_(img):
#     kernel_size = 11 #7
#     kernel = cv2.getStructuringElement(cv2.MORPH_RECT,(kernel_size, 1))
#     #膨胀之后再腐蚀，在用来关闭前景对象里的小洞或小黑点
#     #开运算用于移除由图像噪音形成的斑点
#     opened = cv2.morphologyEx(img, cv2.MORPH_OPEN, kernel)
#     closing = cv2.morphologyEx(img,cv2.MORPH_CLOSE,kernel)
    
# #     kernel_size = 3
# #     plane_blur = cv2.GaussianBlur(closing,(kernel_size, kernel_size), 0)
#     return closing

# def find_line(img, quad_mask):
#     mask = quad_mask.astype(np.uint8)*255
#     img_with_mask = cv2.add(img, np.zeros(np.shape(img), dtype=np.uint8), mask=mask)
# #     kernel_size = 3
# #     plane_blur = cv2.GaussianBlur(img_with_mask,(11, 3), 0)
#     edge = cv2.Canny(img_with_mask, 50, 200, 5)

#     ret,thresh2 = cv2.threshold(img_with_mask,200,255,cv2.THRESH_BINARY_INV)
    
# #     element = cv2.getStructuringElement(cv2.MORPH_RECT, (11, 1))
# #     thresh = cv2.erode(thresh2,element)

    
# #     thresh = cv2.add(thresh2, np.zeros(np.shape(img), dtype=np.uint8), mask=mask)
    
# #     gray = cv2.bilateralFilter(thresh, 5, 100, 100)
    
#     edgeds = cv2.Canny(thresh2, 100, 200)
    
#     edged = cv2.add(edgeds, np.zeros(np.shape(img), dtype=np.uint8), mask=mask)
    
#     line_image = cv2.cvtColor(img_with_mask, cv2.COLOR_GRAY2BGR)
    
    
# #     find horizontal_lines
# #     kernel_length = 5
# #     hori_kernel = cv2.getStructuringElement(cv2.MORPH_RECT, (kernel_length, 1))
# #     # Morphological operation to detect horizontal lines from an image
# #     img_temp2 = cv2.erode(edged, hori_kernel, iterations=3)
# #     horizontal_lines_img = cv2.dilate(img_temp2, hori_kernel, iterations=3)


# #detect bold then set the mask at it => then find the approx in the mask
# #     mask = np.zeros((img.shape))
# #     # Set up the detector with default parameters.
# #     detector = cv2.SimpleBlobDetector_create()

# #     # Detect blobs.
# #     keypoints = detector.detect(gray)

# #     if len(keypoints) > 0:
# #         px = int(keypoints[-1].pt[0])
# #         py = int(keypoints[-1].pt[1])
# #         w = 40
# #         h = 5
# #         cv2.rectangle(mask, (px+w, py+h), (px-w, py-h), 255, -1)


# # contours
#     _, contours, hierarchy = cv2.findContours(edged, cv2.RETR_LIST, cv2.CHAIN_APPROX_SIMPLE)  
#     cnts = sorted(contours, key = cv2.contourArea, reverse = True)[:5]

#     for c in cnts:
#         peri = cv2.arcLength(c, True)
#         approx = cv2.approxPolyDP(c, 0.09 * peri, True)
        
#         if len(approx) == 2 :
#             screenCnt = approx
#             cv2.drawContours(line_image, [screenCnt], -1, (0, 255, 225), 2)
            
            
            
# #    ellipse
# #     for c in cnts:
# #         if len(c) >= 5:
# #             ellipse = cv2.fitEllipse(c)
            
# #             x = ellipse[0][0]
# #             y = ellipse[0][1]
# #             a = ellipse[1][0]
# #             b = ellipse[1][1]
# #             if b > a * 10:
# #                 cv2.ellipse(line_image, ellipse, (0,255,0), 2)
# #                 cv2.line(line_image, (int(x-b/2), int(y)), (int(x+b/2), int(y)), (0, 0, 255), 1)
                

# #     HoughLinesP
# #     minLineLength = 0
# #     maxLineGap = 25
# #     lines = cv2.HoughLinesP(edged,1,np.pi/180,10,minLineLength,maxLineGap)
# #     if lines is not None:
# #         for line in lines:
# #             x1, y1, x2, y2 = line[0]
# #             cv2.line(line_image, (x1, y1), (x2, y2), (0, 0, 255), 2)
            
# #     HoughLines
# #     lines = cv2.HoughLines(edged,1,np.pi/180,35)
# #     if lines is not None:
# #         lines1 = lines[:,0,:]#提取为为二维
# #         for rho,theta in lines1[:]: 
# #             a = np.cos(theta)
# #             b = np.sin(theta)
# #             x0 = a*rho
# #             y0 = b*rho
# #             x1 = int(x0 + 1000*(-b))
# #             y1 = int(y0 + 1000*(a))
# #             x2 = int(x0 - 1000*(-b))
# #             y2 = int(y0 - 1000*(a)) 
# #             cv2.line(line_image,(x1,y1),(x2,y2),(0, 255, 255),1)

#     return line_image
# get_ipython().system('jupyter nbconvert --to script _HMD_Light_function.ipynb')

#20200218 show image 3d target points
import matplotlib.pyplot as plt
from mpl_toolkits.mplot3d import Axes3D

def show_plane(plane_x, plane_y, points_3d):
    line_num = plane_x.shape[0]
    point_num = plane_x.shape[1]
    size = line_num * point_num
    
    x = np.linspace(-1,1,size)
    y = np.linspace(-1,1,size)
    z = np.linspace(-1,1,size)
    
    for i in range(line_num): 
        for j in range(point_num): 
            u = plane_x[i,j]
            v = plane_y[i,j]
            x[i*line_num+j] = points_3d[v,u,0]
            y[i*line_num+j] = points_3d[v,u,1]
            z[i*line_num+j] = points_3d[v,u,2]
    

    fig = plt.figure()
    ax = fig.gca(projection='3d')

    surf = ax.scatter(x, y, z)