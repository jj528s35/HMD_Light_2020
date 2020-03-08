#!/usr/bin/env python
# coding: utf-8

# In[1]:


import socket_sender


# In[2]:


def send_touch(touch, points_3d):
    data = ""
    temp4 = ['1 ']
    touch_num = len(touch)
    temp4.append("%d "%(touch_num))
    for i in range(touch_num): 
        feet_x = touch[i][0]
        feet_y = touch[i][1]
        x = points_3d[feet_y, feet_x,0]
        y = points_3d[feet_y, feet_x,1]
        z = points_3d[feet_y, feet_x,2]
        temp4.append("%f %f %f "%(x, y, z))
    data = data.join(temp4)
#     print('send plane point:', data)
    socket_sender.send(data)
    
    
def receive_data(cam, window_corner):
    stop = False
    data = socket_sender.receive()
    if(data != None):
        #print("receive " + data)
        stop, window_corner = ParseData(data, cam, window_corner)
    return stop, window_corner
        
def ParseData(data, cam, window_corner):
    stop = False
    # split the string at ' '
    msg = data.split(' ')
    if((len(msg)-2 != 12 and len(msg)-1 != 1)):
        return stop, window_corner
    # get the first slice of the list
    data_type = int(msg[0])
    
    if(data_type == 1):# change user case
        fps = int(msg[1])
        change_user_case(fps,cam)
    elif(data_type == 2):
        socket_sender.close_socket()
        stop = True
    elif(data_type == 3):
        if(len(msg)-2 == 12):
            for i in range(4):
                window_corner[i,0] = msg[i*3 + 1]
                window_corner[i,1] = msg[i*3 + 2]
                window_corner[i,2] = msg[i*3 + 3]
            
    
    return stop, window_corner


# data receive from unity

# change the usercase
def change_user_case(fps,cam):
    if(fps == 45):
        cam.setUseCase('MODE_5_45FPS_500')
    elif(fps == 35):
        cam.setUseCase('MODE_5_35FPS_600')
    print("UseCase",cam.getCurrentUseCase())


# In[ ]:




