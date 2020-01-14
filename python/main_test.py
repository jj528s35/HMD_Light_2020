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

try:
    import roypycy
except ImportError:
    print("Pico Flexx backend requirements (roypycy) not installed properly")
    raise

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
            print('store time:', (time.time()-t_time))

    def paint (self, data):
        """Called in the main thread, with data containing one of the items that was added to the
        queue in onNewData.
        """
        name = "depth"
        cv2.namedWindow(name, cv2.WINDOW_NORMAL)
        cv2.imshow(name, data)
        cv2.waitKey(1)
        
#         # create a figure and show the raw data
#         plt.figure(1)
#         plt.imshow(data)

#         plt.show(block = False)
#         plt.draw()
#         # this pause is needed to ensure the drawing for
#         # some backends
#         plt.pause(0.001)

def main ():
    parser = argparse.ArgumentParser (usage = __doc__)
    add_camera_opener_options (parser)
    parser.add_argument ("--seconds", type=int, default=15, help="duration to capture data")
    #options = parser.parse_args(args=['--rrf', '123.rrf','--seconds', '5'])
    options = parser.parse_args(args=['--seconds', '15'])

    opener = CameraOpener (options)
    cam = opener.open_camera ()
    cam.setUseCase('MODE_5_45FPS_500')#MODE_9_5FPS_2000

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
    process_event_queue (x,y,z, l, options.seconds)
    cam.stopCapture()
    
    cv2.destroyAllWindows()
    

def process_event_queue (x,y,z, painter, seconds):
    # create a loop that will run for the given amount of time
    t_end = time.time() + seconds
    while time.time() < t_end:
        try:
            # try to retrieve an item from the queue.
            # this will block until an item can be retrieved
            # or the timeout of 1 second is hit
            t_time = time.time()
            
            item_x = x.get(True, 0.5)
            item_y = y.get(True, 0.5)
            item_z = z.get(True, 0.5)
            points3D = np.dstack((item_x,item_y,item_z))
            print('queue time:', (time.time()-t_time))
        except queue.Empty:
            # this will be thrown when the timeout is hit
            break
        else:
            painter.paint (item_z)
            


# In[18]:


main()




