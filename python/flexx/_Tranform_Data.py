#!/usr/bin/env python
# coding: utf-8

# In[1]:


import socket_sender


# In[2]:


#send plane equation a, b, c, d
def send_plane_eq(plane_eq):
    data = ""
    temp1 = ['1 ']
    temp1.append("%f %f %f %f"%(plane_eq[0], plane_eq[1], plane_eq[2], plane_eq[3]))
    data = data.join(temp1)
#     print('send plane_eq:', data)
    socket_sender.send(data)
    
    #send 3 sample points which found the best plane by RANSAC
def send_sample_points(sample_points):
    data = ""
    temp2 = ['2 ']
    for i in range(3): 
        temp2.append("%f %f %f "%(sample_points[i][0], sample_points[i][1], sample_points[i][2]))
    data = data.join(temp2)
#     print('send sample point:', data)
    socket_sender.send(data)
    
    #send quadrilateral 4 vertices (in would coord)
def send_plane_points(points):
    data = ""
    temp3 = ['3 ']
    point_num = len(points)
    temp3.append("%d "%(point_num))
    for i in range(point_num): 
        x = points[i,0]
        y = points[i,1]
        z = points[i,2]
        temp3.append("%f %f %f "%(x, y, z))
        if i == 3:
            break
    data = data.join(temp3)
#     print('send plane point:', data)
    if point_num > 4:
        print("more than 4 points")
    socket_sender.send(data)
    
    #send quadrilateral center (quadrilateral : large quadrilateral on the mask of plane)
def send_plane_center(x, y, points_3d):
    data = ""
    temp4 = ['4 ']
    temp4.append("%f %f %f"%(points_3d[y,x,0], points_3d[y,x,1], points_3d[y,x,2]))
    data = data.join(temp4)
#     print('send plane point:', data)
    socket_sender.send(data)

    #send little black circle position (in would space)
def send_targetpos(x, y, points_3d):
    data = ""
    temp4 = ['5 ']
    temp4.append("%f %f %f"%(points_3d[y,x,0], points_3d[y,x,1], points_3d[y,x,2]))
    data = data.join(temp4)
#     print('send plane point:', data)
    socket_sender.send(data)
    
    
def send_forward_vector(x, y, fx, fy, points_3d):
    data = ""
    temp4 = ['6 ']
    temp4.append("%f %f %f"%\
                 (points_3d[fy,fx,0]-points_3d[y,x,0], points_3d[fy,fx,1]-points_3d[y,x,1], points_3d[fy,fx,2]-points_3d[y,x,2]))
    data = data.join(temp4)
#     print('send plane point:', data)
    socket_sender.send(data)
    
    
def send_target_plane(plane_x, plane_y, points_3d):
    data = ""
    temp3 = ['7 ']
    line_num = plane_x.shape[0]
    point_num = plane_x.shape[1]
    temp3.append("%d "%(line_num*point_num))
    for i in range(line_num): 
        for j in range(point_num): 
            u = plane_x[i,j]
            v = plane_y[i,j]
            x = points_3d[v,u,0]
            y = points_3d[v,u,1]
            z = points_3d[v,u,2]
            temp3.append("%f %f %f "%(x, y, z))
            
    data = data.join(temp3)
#     print('send plane point:', data)

    socket_sender.send(data)
    
def send_target_plane(plane_p):
    data = ""
    temp3 = ['7 ']
    line_num = plane_p.shape[0]
    point_num = plane_p.shape[1]
    temp3.append("%d "%(line_num*point_num))
    for i in range(line_num): 
        for j in range(point_num): 
            x = plane_p[i,j,0]
            y = plane_p[i,j,1]
            z = plane_p[i,j,2]
            temp3.append("%f %f %f "%(x, y, z))
            
    data = data.join(temp3)
#     print('send plane point:', data)

    socket_sender.send(data)


def send_project_type(_type):
    data = ""
    temp1 = ['8 ']
    temp1.append("%d"%(_type))
    data = data.join(temp1)
#     print('send plane_eq:', data)
    socket_sender.send(data)
    
    
def receive_data(cam):
    stop = False
    data = socket_sender.receive()
    if(data != None):
        print("receive " + data)
        stop = ParseData(data, cam)
    return stop
        
def ParseData(data, cam):
    stop = False
    # split the string at ' '
    msg = data.split(' ')
    # get the first slice of the list
    data_type = int(msg[0])
    
    if(data_type == 1):# change user case
        fps = int(msg[1])
        change_user_case(fps,cam)
    elif(data_type == 2):
        socket_sender.close_socket()
        stop = True
    
    return stop


# data receive from unity

# change the usercase
def change_user_case(fps,cam):
    if(fps == 45):
        cam.setUseCase('MODE_5_45FPS_500')
    elif(fps == 35):
        cam.setUseCase('MODE_5_35FPS_600')
    print("UseCase",cam.getCurrentUseCase())


# In[ ]:




