import numpy as np
import matplotlib.pyplot as plt
import cv2
import math
from matplotlib.path import Path
import _HMD_Light_function

####################################################################################################
#20200224 feet detection 新版演算法
# 依據projector 投影到地面位置y，橫切一條線切割depth image
# 線以上：non VR user
# 線以下: VR user
def get_feet_detection_line(plane_eq):
    a = plane_eq[0]
    b = plane_eq[1]
    c = plane_eq[2]
    d = plane_eq[3]
    normal = math.sqrt(a*a+b*b+c*c)
    if normal == 0:
        normal = 1
    #dis : (0,0,0) to plane
    dis = d/normal
    
    p = np.zeros((3),dtype=float)
    t = dis/normal 
    p[0] = -a*t 
    p[1] = -b*t 
    p[2] = -c*t 
    
    #intrinsic matrix
    fx = 211.787
    fy = 211.6044
    cx = 117.6575
    cy = 87.0219
    
    # reproject point p to pixel coord
    # x' = X/Z   y' = Y/Z
    # u = fx*x'+cx
    if p[2] == 0:
        p[2] = 1
        
    x_ = p[0]/ p[2]
    y_ = p[1]/ p[2]
    u_ = int(round(fx*x_ + cx))
    v_ = int(round(fy*y_ + cy))
    
#     print(u_,v_)
    return v_
    
    

def feet_detection_with_line(depthImg, quad_mask, Confi_mask, top_line, height, feet_height, touch_detect_heigh = 0.1):
    """在Mask內找 10cm > depth > 3cm 
       Mask : Confi_mask and quad_mask (Confi_mask: 去掉雜訊多的部分,  quad_mask: RANSAC找到的平面)
       
    """
#     在Mask內找 10cm > depth > 3cm 
    highter_region = depthImg > height
    lower_region = depthImg < feet_height
    
    feet_region = np.logical_and(highter_region, lower_region)
    feet_region = np.logical_and(feet_region, quad_mask)
    feet_region = np.logical_and(feet_region, Confi_mask)

    
#     VR user
    VR_mask = np.zeros((depthImg.shape))
    VR_mask[top_line:-1,:] = True
    
    VR_feet_region = np.logical_and(feet_region, VR_mask)
        
    VR_region, VR_feet_cnts, VR_feet_center = _find_feet_counter(VR_feet_region, min_area = 30)
    
    VR_feet_image = cv2.cvtColor(VR_region.astype(np.uint8), cv2.COLOR_GRAY2BGR)
    
    if len(VR_feet_center) == 1:
        #維持上一個frame
        cv2.circle(VR_region, (VR_feet_center[0][0],VR_feet_center[0][1]), 2 , (255,255,0) , -1)
        VR_feet_center = []
    elif len(VR_feet_center) >= 2:
        # >= 2個block，取最大兩個
        VR_feet_center = VR_feet_center[:2]
        cv2.circle(VR_feet_image, (VR_feet_center[0][0],VR_feet_center[0][1]), 2 , (255,255,0) , -1)
        cv2.circle(VR_feet_image, (VR_feet_center[1][0],VR_feet_center[1][1]), 2 , (255,255,0) , -1)
    
    
#     Non VR user
    Non_VR_mask = np.zeros((depthImg.shape))
    top_shift = 10
    Non_VR_mask[:top_line-top_shift,:] = True
    
    Non_VR_feet_region = np.logical_and(feet_region, Non_VR_mask)
    
    Non_VR_region, Non_VR_feet_cnts, Non_VR_feet_center = _find_feet_counter(Non_VR_feet_region, min_area = 40)
    
    Non_VR_feet_image = cv2.cvtColor(Non_VR_region.astype(np.uint8), cv2.COLOR_GRAY2BGR)
    
    Non_VR_feet_top = []
    Non_VR_feet_touch = []
    for i in range(len(Non_VR_feet_center)):
        f_top = tuple(Non_VR_feet_cnts[i][:,0][Non_VR_feet_cnts[i][:,:,1].argmax()])
        Non_VR_feet_top.append(f_top)
        if depthImg[f_top[1],f_top[0]] > touch_detect_heigh:# > 5cm
            cv2.circle(Non_VR_feet_image, (f_top[0],f_top[1]), 2 , (255,0,0) , -1)
        else :# <= 5cm
            cv2.circle(Non_VR_feet_image, (f_top[0],f_top[1]), 2 , (0,0,255) , -1)
            Non_VR_feet_touch.append(f_top)
    
    
    # merge 2 image
    image = np.ones((Non_VR_feet_image.shape))*255
    image[top_line:-1,:,:] = VR_feet_image[top_line:-1,:,:]
    image[:top_line-top_shift,:,:] = Non_VR_feet_image[:top_line-top_shift,:,:]
    
    
    return image, VR_feet_image, VR_feet_center, Non_VR_feet_image, Non_VR_feet_top, Non_VR_feet_touch

def _find_feet_counter(feet_region, min_area = 30):
    feet_region_smooth = MorphologyEx(feet_region.astype(np.uint8))
    area = 0
    feet_center = []
    feet_cnts = []
    max_highter_region = np.zeros(feet_region.shape, dtype=np.uint8)
    
    (_, cnts, _) = cv2.findContours(feet_region_smooth.astype(np.uint8)*255, cv2.RETR_LIST, cv2.CHAIN_APPROX_SIMPLE) 
    if len(cnts) >= 1 :
        cnts = sorted(cnts, key = cv2.contourArea, reverse = True)[:2] # different with v1.2
        for c in cnts:
            area = cv2.contourArea(c)
            if area > min_area :
                #依Contours圖形建立mask
                cv2.drawContours(max_highter_region, [c], -1, 255, -1) #255        →白色, -1→塗滿
                # compute the center of the contour
                M = cv2.moments(c)
                cX = int(M["m10"] / M["m00"])
                cY = int(M["m01"] / M["m00"])
                feet_center.append((cX,cY))
                feet_cnts.append(c)
                
    return max_highter_region, feet_cnts, feet_center

#######################################################
#20200308 v2

def boundary_check(x, boundary):
    if x >= boundary:
        x = boundary-1
    if x < 0:
        x = 0
    return x

def MorphologyEx(img):
    kernel_size = 3 #7
    kernel = cv2.getStructuringElement(cv2.MORPH_RECT,(kernel_size, kernel_size))
    #膨胀之后再腐蚀，在用来关闭前景对象里的小洞或小黑点
    #开运算用于移除由图像噪音形成的斑点
    opened = cv2.morphologyEx(img, cv2.MORPH_OPEN, kernel)
    closing = cv2.morphologyEx(img,cv2.MORPH_CLOSE,kernel)
    
    kernel_size = 3
    plane_blur = cv2.GaussianBlur(closing,(kernel_size, kernel_size), 0)
    return plane_blur

def find_window_mask(window_corner, shape):
    corner = camera_2_pixel_coor(window_corner)
    
    #get mask of quadrilateral 
    height = shape[0]
    width = shape[1]
    mask = np.ones((height,width))
    polygon = []
    
    polygon=[(corner[0,1],corner[0,0]), (corner[1,1],corner[1,0]), (corner[2,1],corner[2,0]), (corner[3,1],corner[3,0])]

    poly_path=Path(polygon)

    x, y = np.mgrid[:height, :width]
    coors=np.hstack((x.reshape(-1, 1), y.reshape(-1,1))) # coors.shape is (4000000,2)

    mask = poly_path.contains_points(coors)
    mask = mask.reshape(height, width)
    return mask

def camera_2_pixel_coor(window_corner):
    corner = np.zeros((4,2), dtype=np.uint8)
    
    #intrinsic matrix
    fx = 211.787
    fy = 211.6044
    cx = 117.6575
    cy = 87.0219
    
    for i in range(4):
        new_p = window_corner[i,:]
        # reproject point p to pixel coord
        # x' = X/Z   y' = Y/Z
        # u = fx*x'+cx
        x_ = new_p[0]/ new_p[2]
        y_ = new_p[1]/ new_p[2]
        u_ = int(round(fx*x_ + cx))
        v_ = int(round(fy*y_ + cy))
        
#         print(u_, v_)

        #boundary
        u_ = boundary_check(u_, 224)
        v_ = boundary_check(v_, 171)
        
        corner[i,0] = u_
        corner[i,1] = v_
        
    return corner
        

def feet_detection(depthImg, window_mask, Confi_mask, height, feet_height, touch_detect_heigh = 0.1):
    """在Mask內找 10cm > depth > 3cm 
       Mask : Confi_mask and quad_mask (Confi_mask: 去掉雜訊多的部分,  quad_mask: RANSAC找到的平面)
       
    """
#     在Mask內找 10cm > depth > 3cm 
    highter_region = depthImg > height
    lower_region = depthImg < feet_height
    
    feet_region = np.logical_and(highter_region, lower_region)
    feet_region = np.logical_and(feet_region, window_mask)
    feet_region = np.logical_and(feet_region, Confi_mask)

    VR_region, VR_feet_cnts, VR_feet_center = _find_feet_counter(feet_region, min_area = 30)
    
    VR_feet_image = cv2.cvtColor(VR_region.astype(np.uint8), cv2.COLOR_GRAY2BGR)
    
    
    feet_top = []
    feet_touch = []
    for i in range(len(VR_feet_center)):
        f_top = tuple(VR_feet_cnts[i][:,0][VR_feet_cnts[i][:,:,1].argmax()])
        feet_top.append(f_top)
        if depthImg[f_top[1],f_top[0]] > touch_detect_heigh:# > 5cm
            cv2.circle(VR_feet_image, (f_top[0],f_top[1]), 2 , (255,255,0) , -1)
        else :# <= 5cm
            cv2.circle(VR_feet_image, (f_top[0],f_top[1]), 2 , (0,0,255) , -1)
            feet_touch.append(f_top)
    
    
    
    return VR_feet_image, feet_top, feet_touch