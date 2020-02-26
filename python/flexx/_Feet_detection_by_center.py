import numpy as np
import matplotlib.pyplot as plt
import cv2
import math
from matplotlib.path import Path
import _HMD_Light_function

####################################################################################################
#20200223 feet detection 第二版演算法
#依照blob數目決定
#blob == 1 維持上一個frame
#blob == 2 and y距離<40 更新vr_user_feet_center位置 & feet_mask
#blob == 3 
#          VR user: 在上一個frame中的feet_mask中找feet，若成功找到2個blob，更新vr_user_feet_center位置 & feet_mask
#          non VR user: 在feet_mask_inv 中找feet，找feet最低點，視為non VR user feet top
def feet_detection(depthImg, quad_mask, last_feet_mask, Confi_mask, height, feet_height):
    """在Mask內找 10cm > depth > 3cm 
       Mask : Confi_mask and quad_mask (Confi_mask: 去掉雜訊多的部分,  quad_mask: RANSAC找到的平面)
       
    """
#     在Mask內找 10cm > depth > 3cm 
    highter_region = depthImg > height
    lower_region = depthImg < feet_height
    
    feet_region = np.logical_and(highter_region, lower_region)
    feet_region = np.logical_and(feet_region, quad_mask)
    feet_region = np.logical_and(feet_region, Confi_mask)
    
#      max_highter_region : smooth the feet_region and find the mask of the max 5 contourArea in feet_region
#     Smooth the feet_region => 找面積 > 30的block
#     Output: block數目，block中心點，block Contours
    feet_region_smooth = MorphologyEx(feet_region.astype(np.uint8))
    area = 0
    feet_center = []
    feet_cnts = []
    max_highter_region = np.zeros(depthImg.shape, dtype=np.uint8)
    image = cv2.cvtColor(max_highter_region.astype(np.uint8)*255, cv2.COLOR_GRAY2BGR)
    
    (_, cnts, _) = cv2.findContours(feet_region_smooth.astype(np.uint8)*255, cv2.RETR_LIST, cv2.CHAIN_APPROX_SIMPLE) 
    if len(cnts) >= 1 :
        cnts = sorted(cnts, key = cv2.contourArea, reverse = True)[:5]
        for c in cnts:
            area = cv2.contourArea(c)
            if area > 30 :
                #依Contours圖形建立mask
                cv2.drawContours(max_highter_region, [c], -1, 255, -1) #255        →白色, -1→塗滿
                # compute the center of the contour
                M = cv2.moments(c)
                cX = int(M["m10"] / M["m00"])
                cY = int(M["m01"] / M["m00"])
                feet_center.append((cX,cY))
                feet_cnts.append(c)
     
    
    image = cv2.cvtColor(max_highter_region, cv2.COLOR_GRAY2BGR)
    feet_mask = np.zeros(depthImg.shape)
    new_feet_mask = False
    Non_VR_feet_top = []
    
    if len(feet_center) == 1:
        #維持上一個frame
        cv2.circle(image, (feet_center[0][0],feet_center[0][1]), 2 , (0,255,0) , -1)
        feet_center = []
        
    if len(feet_center) == 2:
        x1 = feet_center[0][0]
        y1 = feet_center[0][1]
        x2 = feet_center[1][0]
        y2 = feet_center[1][1]
        if (x1 - x2) == 0:
            slope = 1
        else:
            slope = (y1 - y2)/(x1 - x2)
        if abs(y1 - y2) < 40:#abs(slope) < 0.6: #雙腳平行
            new_feet_mask = True
            feet_mask = get_feet_mask(feet_cnts, depthImg.shape)

            for i in range(len(feet_center)):
                cv2.circle(image, (feet_center[i][0],feet_center[i][1]), 2 , (255,255,0) , -1)
        else:
            #兩腳y相差太大 => 行走或非VR user雙腳 => 維持上一frame
            feet_center = []
                
    elif len(feet_center) > 2:
        #VR 在last_feet_mask內再找一次腳，若找到2隻腳 => 更新feet mask
        feet_center = []
        ret,thresh = cv2.threshold(max_highter_region,200,True,cv2.THRESH_BINARY)
        VR_feet_region = np.logical_and(thresh, last_feet_mask)
        VR_region, VR_feet_cnts, VR_feet_center = find_feet_counter(VR_feet_region)
        new_feet_mask = True
        if len(VR_feet_cnts) == 2:
            feet_mask = get_feet_mask(VR_feet_cnts, depthImg.shape)
            feet_center = VR_feet_center
            for i in range(len(VR_feet_cnts)):
                cv2.circle(image, (VR_feet_center[i][0],VR_feet_center[i][1]), 2 , (255,255,0) , -1)
        
        #Non VR
        # Non_VR_mask : feet mask最上方以上
        #在Non_VR_mask內再找一次腳
        top = last_feet_mask.argmax()//last_feet_mask.shape[1]
        top_shift = 10
        top = top - top_shift
        Non_VR_mask = np.zeros((last_feet_mask.shape))
        Non_VR_mask[:top,:] = True
        Non_VR_feet_region = np.logical_and(thresh, Non_VR_mask)
        
        Non_VR_region, Non_VR_feet_cnts, Non_VR_feet_center = find_feet_counter(Non_VR_feet_region, min_area = 40)
        for i in range(len(Non_VR_feet_center)):
#             cv2.circle(image, (Non_VR_feet_center[i][0],Non_VR_feet_center[i][1]), 2 , (255,0,255) , -1)
            f_top = tuple(Non_VR_feet_cnts[i][:,0][Non_VR_feet_cnts[i][:,:,1].argmax()])
            Non_VR_feet_top.append(f_top)
            cv2.circle(image, (f_top[0],f_top[1]), 2 , (0,0,255) , -1)
        
#         cv2.namedWindow("Non_VR_feet_region", cv2.WINDOW_NORMAL)
#         cv2.imshow("Non_VR_feet_region", Non_VR_mask.astype(np.uint8)*255)
#         cv2.waitKey(1)

    return image, max_highter_region, new_feet_mask, feet_mask, feet_center, Non_VR_feet_top 

def get_feet_mask(feet_cnts, shape):
    c = feet_cnts[0]
    leftmost = c[:,:,0].min()
    rightmost = c[:,:,0].max()
    top = c[:,:,1].min()
    down = c[:,:,1].max()

    c = feet_cnts[1]
    if c[:,:,0].min() < leftmost:
        leftmost = c[:,:,0].min()
    if c[:,:,0].max() > rightmost:
        rightmost = c[:,:,0].max()
    if c[:,:,1].min() < top:
        top = c[:,:,1].min()
    if c[:,:,1].max() > down:
        down = c[:,:,1].max()
        
    
    height = shape[0]
    width = shape[1]
    mask = np.ones((height,width))
    polygon = []
    
    polygon=[(top,leftmost), (top,rightmost), (down,rightmost), (down,leftmost)]
    poly_path=Path(polygon)

    x, y = np.mgrid[:height, :width]
    coors=np.hstack((x.reshape(-1, 1), y.reshape(-1,1))) # coors.shape is (4000000,2)

    mask = poly_path.contains_points(coors)
    mask = mask.reshape(height, width)
    
    return mask

def find_feet_counter(feet_region, min_area = 30):
    feet_region_smooth = MorphologyEx(feet_region.astype(np.uint8))
    area = 0
    feet_center = []
    feet_cnts = []
    max_highter_region = np.zeros(feet_region.shape, dtype=np.uint8)
    
    (_, cnts, _) = cv2.findContours(feet_region_smooth.astype(np.uint8)*255, cv2.RETR_LIST, cv2.CHAIN_APPROX_SIMPLE) 
    if len(cnts) >= 1 :
        cnts = sorted(cnts, key = cv2.contourArea, reverse = True)[:5]
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

# def feet_center_detection(depthImg, quad_mask, Confi_mask, height, feet_height, VR_user = True):
#     """ find the feet mask and ellipse_list"""
#     """ 
#         Need Interaction
#         VR_user: find the feet top(Ellipses up) in lower quad mask
#         non VR_user: find the feet top(Ellipses down) in upper quad mask
#     """
#     if VR_user:
#         mask = np.zeros(depthImg.shape)
#         mask[0:depthImg.shape[0]//2,:] = False
#         mask[depthImg.shape[0]//2:-1,:] = True
#         quad_mask = np.logical_and(quad_mask, mask)
#     else:
#         mask = np.zeros(depthImg.shape)
#         mask[0:depthImg.shape[0]//2,:] = True
#         mask[depthImg.shape[0]//2:-1,:] = False
#         quad_mask = np.logical_and(quad_mask, mask)
    
#     #find the highter_region mask which distance between resulting plane is > height
#     #find the lower_region mask which distance between resulting plane is < feet_height
#     #feet_region : region which distance between resulting plane is < feet_height and > height
#     #Then find the feet_region : the feet_region which is inside the quad mask
    
#     highter_region = depthImg > height
#     lower_region = depthImg < feet_height
    
#     feet_region = np.logical_and(highter_region, lower_region)
#     feet_region = np.logical_and(feet_region, quad_mask)
#     feet_region = np.logical_and(feet_region, Confi_mask)
    
#     #max_highter_region : smooth the feet_region and find the mask of the max 5 contourArea in feet_region
#     feet_region_smooth = MorphologyEx(feet_region.astype(np.uint8))
#     area = 0
#     feet_center = []
#     max_highter_region = np.zeros(depthImg.shape, dtype=np.uint8)
#     image = cv2.cvtColor(max_highter_region.astype(np.uint8)*255, cv2.COLOR_GRAY2BGR)
    
#     (_, cnts, _) = cv2.findContours(feet_region_smooth.astype(np.uint8)*255, cv2.RETR_LIST, cv2.CHAIN_APPROX_SIMPLE) 
#     if len(cnts) >= 1 :
#         cnts = sorted(cnts, key = cv2.contourArea, reverse = True)[:2]
#         for c in cnts:
#             area = cv2.contourArea(c)
#             if area > 30 :
#                 #依Contours圖形建立mask
#                 cv2.drawContours(max_highter_region, [c], -1, 255, -1) #255        →白色, -1→塗滿
#                 # compute the center of the contour
#                 M = cv2.moments(c)
#                 cX = int(M["m10"] / M["m00"])
#                 cY = int(M["m01"] / M["m00"])
#                 feet_center.append((cX,cY))
     
    
#     image = cv2.cvtColor(max_highter_region, cv2.COLOR_GRAY2BGR)
#     for i in range(len(feet_center)):
#         cv2.circle(image, (feet_center[i][0],feet_center[i][1]), 2 , (255,255,0) , -1)

#     return image, max_highter_region, feet_center 



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


def find_center_and_vector_by_top(feet_top, img):
    success = False
    minX = 0
    minY = 0
    maxX = 0
    maxY = 0
    centerX = 0
    centerY = 0
    if(len(feet_top) == 2):
        success = True
        if feet_top[0][0] < feet_top[1][0]:
            minX = feet_top[0][0] 
            minY = feet_top[0][1] 
            maxX = feet_top[1][0]
            maxY = feet_top[1][1]
        else:
            minX = feet_top[1][0] 
            minY = feet_top[1][1] 
            maxX = feet_top[0][0]
            maxY = feet_top[0][1]
            
        
        #boundary
        maxX = boundary_check(maxX, img.shape[1])
        maxY = boundary_check(maxY, img.shape[0])
        minX = boundary_check(minX, img.shape[1])
        minY = boundary_check(minY, img.shape[0])
        
        centerX = int(round((minX + maxX)/2))
        centerY = int(round((minY + maxY)/2))
        cv2.circle(img, (centerX,centerY), 2 , (0,255,255) , -1)
        
    return success, centerX, centerY, minX, minY, maxX, maxY, img

def boundary_check(x, boundary):
    if x >= boundary:
        x = boundary-1
    if x < 0:
        x = 0
    return x

def find_plane_center(centerX, centerY, minX, minY, maxX, maxY, img, points_3d, plane_mask, plane_size = 0.1):
    success = True
    if (maxX - minX) == 0:
        slope = (maxY - minY)/1
    else:
        slope = (maxY - minY)/(maxX - minX)
        
    if slope == 0:
        slope = 1/(maxX - minX)
        
    V_slope = -1/slope
    
    slope_dist = np.sqrt(1 + V_slope*V_slope)
    #dist between a and a' is 10 pixels
    dist = 20 # 10 pixel
    t = dist/slope_dist

    # point a' position in pixel coord
    if(V_slope > 0):
        _nx = int(centerX - round(1 * t))
        _ny = int(centerY - round(V_slope * t))
    else:
        _nx = int(centerX + round(1 * t))
        _ny = int(centerY + round(V_slope * t))
        
        
    real_dist = plane_size/2 # 3 cm
    #intrinsic matrix
    fx = 211.787
    fy = 211.6044
    cx = 117.6575
    cy = 87.0219
    v = np.zeros((3),dtype=float)
    new_p = np.zeros((3),dtype=float) #new 3d point which 
    #new line which dist between origional one is 5 cm
    _x = centerX
    _y = centerY

    #boundary
    _nx = boundary_check(_nx, img.shape[1])
    _ny = boundary_check(_ny, img.shape[0])

#     #check if it have value
    if(plane_mask[_y,_x] == False):
        _x, _y = _HMD_Light_function.find_points_on_plane(_y,_x, plane_mask, kernel_size = 5)
        if(plane_mask[_y,_x] == False):
#             print("1")
            success = False
#         else:
#             cv2.circle(img, (_x,_y), 2 , (0,255,255) , -1)

    if(plane_mask[_ny,_nx] == False):
        _nx, _ny = _HMD_Light_function.find_points_on_plane(_ny,_nx, plane_mask, kernel_size = 5)
        if(plane_mask[_ny,_nx] == False):
#             print("2")
            success = False
     
    #     cv2.circle(img, (_nx, _ny), 2 , (0,0,255) , -1)

    if success == False:
        return success, img, 0, 0
    
    # vector from a to a' in 3d coord
    v[0] = points_3d[_ny,_nx,0]-points_3d[_y,_x,0]
    v[1] = points_3d[_ny,_nx,1]-points_3d[_y,_x,1]
    v[2] = points_3d[_ny,_nx,2]-points_3d[_y,_x,2]
    #dist between a to a' in 3d coord
    v_dist = np.sqrt(v[0]*v[0]+v[1]*v[1]+v[2]*v[2])

    # point p which is 5 cm from the point a with vector v
    _t = real_dist/v_dist
    new_p[0] = points_3d[_y,_x,0] + v[0] * _t
    new_p[1] = points_3d[_y,_x,1] + v[1] * _t
    new_p[2] = points_3d[_y,_x,2] + v[2] * _t

    # reproject point p to pixel coord
    # x' = X/Z   y' = Y/Z
    # u = fx*x'+cx
    if new_p[2] == 0:
        new_p[2] = 1
        
    x_ = new_p[0]/ new_p[2]
    y_ = new_p[1]/ new_p[2]
    u_ = int(round(fx*x_ + cx))
    v_ = int(round(fy*y_ + cy))
    
    cv2.circle(img, (u_,v_), 2 , (0,255,0) , -1)
    
    #boundary
    u_ = boundary_check(u_, img.shape[1])
    v_ = boundary_check(v_, img.shape[0])
        
    return success, img, u_,v_


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
