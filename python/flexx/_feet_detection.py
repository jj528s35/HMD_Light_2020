import numpy as np
import matplotlib.pyplot as plt
import cv2
import _HMD_Light_function

def feet_detection(depthImg, quad_mask, height = 0.05, feet_height = 0.1):
    #find the highter_region mask which distance between resulting plane is > height
    #Then find the highter_region_in_quad : the higher region which is inside the quad mask
    #find the lower_region mask which distance between resulting plane is < feet_height
    #feet_region : max_highter_region which distance between resulting plane is < feet_height
    
    highter_region = depthImg > height
    lower_region = depthImg < feet_height
    
    feet_region = np.logical_and(highter_region, lower_region)
    feet_region = np.logical_and(feet_region, quad_mask)
    
    #find the mask of the max contourArea in highter_region_in_quad ==> find the body, leg, and feet
    feet_region_smooth = MorphologyEx(feet_region.astype(np.uint8))
    area = 0
    max_highter_region = np.zeros(depthImg.shape)
    (_, cnts, _) = cv2.findContours(feet_region_smooth.astype(np.uint8)*255, cv2.RETR_LIST, cv2.CHAIN_APPROX_SIMPLE) 
    if len(cnts) >= 1 :
        cnts = sorted(cnts, key = cv2.contourArea, reverse = True)[:5]
        for c in cnts:
            area = cv2.contourArea(c)
            if area > 40 :#and area < 200:
                #依Contours圖形建立mask
                cv2.drawContours(max_highter_region, [c], -1, True, -1) #255        →白色, -1→塗滿
                
    
    feet_region_with_Ellipses, ellipse_list = fit_Ellipses(max_highter_region.astype(np.uint8))#feet_region_smooth
    feet_region_with_Ellipses, feet_top = find_top(feet_region_with_Ellipses, ellipse_list)
    
    
    return feet_region_with_Ellipses, max_highter_region, ellipse_list, feet_top



def fit_Ellipses(feet_region):
    image = cv2.cvtColor(feet_region.astype(np.uint8)*255, cv2.COLOR_GRAY2BGR)
    feet_mask = np.zeros(feet_region.shape)
    ellipse_List = []
    (_, cnts, _) = cv2.findContours(feet_region, cv2.RETR_LIST, cv2.CHAIN_APPROX_SIMPLE)
    cnts = sorted(cnts, key = cv2.contourArea, reverse = True)[:2]
    
    # loop over our contours
    for c in cnts:
        area = cv2.contourArea(c)
        if area > 40 and len(c) >= 5:
            ellipse = cv2.fitEllipse(c)
            Area = Ellipse_area(ellipse[1][0], ellipse[1][1])
            if Area > 40 :#and area/Area > 0.6:
                ellipse_List.append(cv2.fitEllipse(c))
                cv2.ellipse(image, ellipse, (0,255,255), 2)
                cv2.ellipse(feet_mask, ellipse, 255, -1)
            else:
                print(Area,"1")

    return feet_mask, ellipse_List #image

def Ellipse_area(a2, b2):
    a = a2 / 2
    b = b2 / 2
    Area = 3.142 * a * b 
    
#     #長寬比
#     if( a > b*2.5 or b > a*2.5):
#         return 0

    return Area 

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


def find_top(image, ellipse_List):
    #         https://stackoverflow.com/questions/33432652/how-draw-axis-of-ellipse?fbclid=IwAR2l6knFBSlRjP7hg2chCR-8IF5gPvCGtkwUWyZ0Mnt3yoYbBAGVAkMVP1w
    image = cv2.cvtColor(image.astype(np.uint8), cv2.COLOR_GRAY2BGR)
    feet_top = []
    for ellipse in ellipse_List:
        a = ellipse[1][0] / 2
        b = ellipse[1][1] / 2
        theta = ellipse[2]* np.pi / 180.0
        
        if np.cos(theta) < 0:
            x = int(ellipse[0][0] - round(b * np.sin(theta)))
            y = int(ellipse[0][1] + round(b * np.cos(theta)))
        elif np.cos(theta) > 0:
            x = int(ellipse[0][0] + round(b * np.sin(theta)))
            y = int(ellipse[0][1] - round(b * np.cos(theta)))
        
        feet_top.append((x,y))
        cv2.circle(image, (x,y), 2 , (255,255,0) , -1)
    return image, feet_top

def find_center_and_vector_by_top(feet_top, img, plane_mask):
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
            
        centerX = int(round((minX + maxX)/2))
        centerY = int(round((minY + maxY)/2))
        cv2.circle(img, (centerX,centerY), 2 , (0,255,255) , -1)
        
        #boundary
        if maxX > img.shape[1]:
            maxX = img.shape[1]
            
        if maxY > img.shape[0]:
            maxY = img.shape[0]
            
        if minX <= 0:
            minX = 0
            
        if minY <= 0:
            minY = 0
        
        if(plane_mask[maxY,maxX] == False):
            maxX, maxY = _HMD_Light_function.find_points_on_plane(maxY, maxX, plane_mask, kernel_size = 7)
            
        if(plane_mask[minY,minX] == False):
            minX, minY = _HMD_Light_function.find_points_on_plane(minY, minX, plane_mask, kernel_size = 7)
        
    return success, centerX, centerY, minX, minY, maxX, maxY, img

def find_plane_center(centerX, centerY, minX, minY, maxX, maxY, img, points_3d, plane_size = 0.1):
    slope = (maxY - minY)/(maxX - minX)
    if slope == 0:
        slope = 1
        
    V_slope = -1/slope
    
    slope_dist = np.sqrt(1 + V_slope*V_slope)
    #dist between a and a' is 10 pixels
    dist = 10 # 10 pixel
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
    if _nx >= img.shape[1]:
        _nx = img.shape[1]-1

    if _ny >= img.shape[0]:
        _ny = img.shape[0]-1

#     #check if it have value
#     if(plane_mask[_y,_x] == False):
#         _x, _y = find_points_on_plane(_y,_x, plane_mask, kernel_size = 7)

#     if(plane_mask[_ny,_nx] == False):
#         _nx, _ny = find_points_on_plane(_ny,_nx, plane_mask, kernel_size = 7)

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
    return img, u_,v_

######20200123
def _feet_detection(depthImg, mask_successful, quad_mask, height = 0.05, feet_height = 0.1):
    #find the highter_region mask which distance between resulting plane is > height
    #Then find the highter_region_in_quad : the higher region which is inside the quad mask
    highter_region = depthImg > height
    if (mask_successful):
        highter_region_in_quad = np.logical_and(highter_region, quad_mask)
    else:
        highter_region_in_quad = highter_region
        ellipse_list = []
        feet_region_with_Ellipses = np.zeros(depthImg.shape)
        return feet_region_with_Ellipses, ellipse_list
    
    #find the mask of the max contourArea in highter_region_in_quad ==> find the body, leg, and feet
    area = 0
    max_highter_region = np.zeros(depthImg.shape)
    (_, cnts, _) = cv2.findContours(highter_region_in_quad.astype(np.uint8)*255, cv2.RETR_LIST, cv2.CHAIN_APPROX_SIMPLE) 
    if len(cnts) >= 1 :
        cnts = sorted(cnts, key = cv2.contourArea, reverse = True)[:2]
        for c in cnts:
            area = cv2.contourArea(c)
            if area > 1000: #the body, leg, and feet area > 1000
                #依Contours圖形建立mask
                cv2.drawContours(max_highter_region, [c], -1, True, -1) #255        →白色, -1→塗滿
        
    
    #find the lower_region mask which distance between resulting plane is < feet_height
    #feet_region : max_highter_region which distance between resulting plane is < feet_height
    lower_region = depthImg < feet_height
    feet_region = np.logical_and(max_highter_region, lower_region)
    
    feet_region_smooth = MorphologyEx(feet_region.astype(np.uint8))
    
    feet_region_with_Ellipses, ellipse_list = fit_Ellipses(feet_region_smooth)
    
    return feet_region_with_Ellipses, ellipse_list


