import numpy as np
import matplotlib.pyplot as plt
import cv2
import _HMD_Light_function

def feet_detection(depthImg, quad_mask, height, feet_height):
    """ find the feet mask and ellipse_list"""
    #find the highter_region mask which distance between resulting plane is > height
    #find the lower_region mask which distance between resulting plane is < feet_height
    #feet_region : region which distance between resulting plane is < feet_height and > height
    #Then find the feet_region : the feet_region which is inside the quad mask
    
    highter_region = depthImg > height
    lower_region = depthImg < feet_height
    
    feet_region = np.logical_and(highter_region, lower_region)
    feet_region = np.logical_and(feet_region, quad_mask)
    
    #max_highter_region : smooth the feet_region and find the mask of the max 5 contourArea in feet_region
    feet_region_smooth = MorphologyEx(feet_region.astype(np.uint8))
    area = 0
    max_highter_region = np.zeros(depthImg.shape, dtype=np.uint8)
    (_, cnts, _) = cv2.findContours(feet_region_smooth.astype(np.uint8)*255, cv2.RETR_LIST, cv2.CHAIN_APPROX_SIMPLE) 
    if len(cnts) >= 1 :
        cnts = sorted(cnts, key = cv2.contourArea, reverse = True)[:5]
        for c in cnts:
            area = cv2.contourArea(c)
            if area > 30 :
                #依Contours圖形建立mask
                cv2.drawContours(max_highter_region, [c], -1, 255, -1) #255        →白色, -1→塗滿
                
#     fit the ellipse in max_highter_region, feet_region_with_Ellipses : mask of ellipse
    feet_region_with_Ellipses, ellipse_list = fit_Ellipses(max_highter_region.astype(np.uint8))

    return feet_region_with_Ellipses, max_highter_region, ellipse_list 


def feet_top_detection(depthImg, quad_mask, height = 0.05, feet_height = 0.1, VR_user = True):
    """ 
        Need Interaction
        VR_user: find the feet top(Ellipses up) in lower quad mask
        non VR_user: find the feet top(Ellipses down) in upper quad mask
    """
    if VR_user:
        mask = np.zeros(depthImg.shape)
        mask[0:depthImg.shape[0]//2,:] = False
        mask[depthImg.shape[0]//2:-1,:] = True
        quad_mask = np.logical_and(quad_mask, mask)
    else:
        mask = np.zeros(depthImg.shape)
        mask[0:depthImg.shape[0]//2,:] = True
        mask[depthImg.shape[0]//2:-1,:] = False
        quad_mask = np.logical_and(quad_mask, mask)
    
    #     feet detection
    feet_region_with_Ellipses, max_highter_region, ellipse_list = \
        feet_detection(depthImg, quad_mask, height, feet_height)
    #     find the top of feet
    feet_region_with_Ellipses, feet_top = find_top(feet_region_with_Ellipses, ellipse_list, VR_user = True)
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
        if area > 30 and len(c) >= 5:
            ellipse = cv2.fitEllipse(c)
            Area = Ellipse_area(ellipse[1][0], ellipse[1][1])
            if Area > 30 :#and area/Area > 0.6:
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

    return Area 

def MorphologyEx(img):
    kernel_size = 3 #7
    kernel = cv2.getStructuringElement(cv2.MORPH_RECT,(kernel_size, kernel_size))
    #膨胀之后再腐蚀，在用来关闭前景对象里的小洞或小黑点
    #开运算用于移除由图像噪音形成的斑点
    opened = cv2.morphologyEx(img, cv2.MORPH_OPEN, kernel)
    closing = cv2.morphologyEx(img,cv2.MORPH_CLOSE,kernel)
    
    kernel_size = 5
    plane_blur = cv2.GaussianBlur(closing,(kernel_size, kernel_size), 0)
    return plane_blur


def find_top(image, ellipse_List, VR_user = True):
    #         https://stackoverflow.com/questions/33432652/how-draw-axis-of-ellipse?fbclid=IwAR2l6knFBSlRjP7hg2chCR-8IF5gPvCGtkwUWyZ0Mnt3yoYbBAGVAkMVP1w
    image = cv2.cvtColor(image.astype(np.uint8), cv2.COLOR_GRAY2BGR)
    feet_top = []
    
    if VR_user:
        top = 1
    else:
        top = -1
    
    for ellipse in ellipse_List:
        a = ellipse[1][0] / 2
        b = ellipse[1][1] / 2
        theta = ellipse[2]* np.pi / 180.0
        
        
        if np.cos(theta) < 0:
            x = int(ellipse[0][0] - top*round(b * np.sin(theta)))
            y = int(ellipse[0][1] + top*round(b * np.cos(theta)))
        elif np.cos(theta) > 0:
            x = int(ellipse[0][0] + top*round(b * np.sin(theta)))
            y = int(ellipse[0][1] - top*round(b * np.cos(theta)))
        
        feet_top.append((x,y))
        cv2.circle(image, (x,y), 2 , (255,255,0) , -1)
    return image, feet_top

# for go forward type
def find_feet_list(image, ellipse_List):
    #         https://stackoverflow.com/questions/33432652/how-draw-axis-of-ellipse?fbclid=IwAR2l6knFBSlRjP7hg2chCR-8IF5gPvCGtkwUWyZ0Mnt3yoYbBAGVAkMVP1w
    feet_list = []
    
    if(len(ellipse_List) == 1):#單腳
        image, feet_list = find_single_feet_left_right(image, ellipse_List[0])
    
    elif(len(ellipse_List) == 2):
        ellipse = ellipse_List[0]
        ellipse1 = ellipse_List[1]
        
        e_x = ellipse[0][0]
        e_y = ellipse[0][1]
        e1_x = ellipse1[0][0]
        e1_y = ellipse1[0][1]
        
        slope = (e_y - e1_y)/(e_x - e1_x)
        if abs(slope) < 0.5: #雙腳平行
            image, feet_list = find_top(image, ellipse_List, VR_user = True)
        else: #左右差太多距離...選前面的
            if e_y < e1_y:
                image, feet_list = find_single_feet_left_right(image, ellipse_List[0])
            else:
                image, feet_list = find_single_feet_left_right(image, ellipse_List[1])

    return image, feet_list

def find_single_feet_left_right(image, ellipse):
    image = cv2.cvtColor(image.astype(np.uint8), cv2.COLOR_GRAY2BGR)
    feet_list = []

    a = ellipse[1][0] / 2
    b = ellipse[1][1] / 2
    theta = ellipse[2]* np.pi / 180.0

    # left one
    x = int(ellipse[0][0] - round(a * np.cos(theta)))
    y = int(ellipse[0][1] - round(a * np.sin(theta)))
    feet_list.append((x,y))
    cv2.circle(image, (x,y), 2 , (255,0,255) , -1)

    # right one
    x = int(ellipse[0][0] + round(a * np.cos(theta)))
    y = int(ellipse[0][1] + round(a * np.sin(theta)))
    feet_list.append((x,y))
    cv2.circle(image, (x,y), 2 , (255,0,255) , -1)
    return image, feet_list

def feet_list_detection(depthImg, quad_mask, height = 0.05, feet_height = 0.1):
    """ go forward type
    """
    #     feet detection
    feet_region_with_Ellipses, max_highter_region, ellipse_list = \
        feet_detection(depthImg, quad_mask, height, feet_height)
    #     find the list of feet
    feet_region_with_Ellipses, feet_list = find_feet_list(feet_region_with_Ellipses, ellipse_list)
    return feet_region_with_Ellipses, max_highter_region, ellipse_list, feet_list
# for go forward type


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
        slope = 1
    else:
        slope = (maxY - minY)/(maxX - minX)
    if slope == 0:
        slope = 1
        
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
        _x, _y = _HMD_Light_function.find_points_on_plane(_y,_x, plane_mask, kernel_size = 7)
        if(plane_mask[_y,_x] == False):
            success = False

    if(plane_mask[_ny,_nx] == False):
        _nx, _ny = _HMD_Light_function.find_points_on_plane(_ny,_nx, plane_mask, kernel_size = 7)
        if(plane_mask[_ny,_nx] == False):
            success = False

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



