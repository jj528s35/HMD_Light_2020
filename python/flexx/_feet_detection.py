import numpy as np
import matplotlib.pyplot as plt
import cv2

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
            if area > 40 and area < 200:
                #依Contours圖形建立mask
                cv2.drawContours(max_highter_region, [c], -1, True, -1) #255        →白色, -1→塗滿
                
    
    feet_region_with_Ellipses, ellipse_list = fit_Ellipses(max_highter_region.astype(np.uint8))#feet_region_smooth
    feet_region_with_Ellipses = find_top(feet_region_with_Ellipses, ellipse_list)
    
    return feet_region_with_Ellipses, ellipse_list



def fit_Ellipses(feet_region):
    image = cv2.cvtColor(feet_region.astype(np.uint8)*255, cv2.COLOR_GRAY2BGR)
    feet_mask = np.zeros(feet_region.shape)
    ellipse_List = []
    (_, cnts, _) = cv2.findContours(feet_region, cv2.RETR_LIST, cv2.CHAIN_APPROX_SIMPLE)
    cnts = sorted(cnts, key = cv2.contourArea, reverse = True)
    
    # loop over our contours
    for c in cnts:
        area = cv2.contourArea(c)
        if area > 40 and len(c) >= 5:
            ellipse = cv2.fitEllipse(c)
            Area = Ellipse_area(ellipse[1][0], ellipse[1][1])
            if Area > 40 and area/Area > 0.6:
                ellipse_List.append(cv2.fitEllipse(c))
                cv2.ellipse(image, ellipse, (0,255,255), 2)
                cv2.ellipse(feet_mask, ellipse, 255, -1)

    return feet_mask, ellipse_List #image

def Ellipse_area(a2, b2):
    a = a2 / 2
    b = b2 / 2
    Area = 3.142 * a * b 
    
    #長寬比
    if( a > b*2.5 or b > a*2.5):
        return 0

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
    image = cv2.cvtColor(image.astype(np.uint8), cv2.COLOR_GRAY2BGR)
    for ellipse in ellipse_List:
        a = ellipse[1][0] / 2
        b = ellipse[1][1] / 2
        theta = ellipse[2]* np.pi / 180.0
        
#         https://stackoverflow.com/questions/33432652/how-draw-axis-of-ellipse?fbclid=IwAR2l6knFBSlRjP7hg2chCR-8IF5gPvCGtkwUWyZ0Mnt3yoYbBAGVAkMVP1w
        if np.cos(theta) < 0:
            x = int(ellipse[0][0] - round(b * np.sin(theta)))
            y = int(ellipse[0][1] + round(b * np.cos(theta)))
        elif np.cos(theta) > 0:
            x = int(ellipse[0][0] + round(b * np.sin(theta)))
            y = int(ellipse[0][1] - round(b * np.cos(theta)))
            
        cv2.circle(image, (x,y), 2 , (255,255,0) , -1)
    return image

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


