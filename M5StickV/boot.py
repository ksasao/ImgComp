import audio
import gc
import image
import lcd
import sensor
import time
import uos
from fpioa_manager import *
from machine import I2C
from Maix import I2S, GPIO

lcd.init()
lcd.rotation(2)
i2c = I2C(I2C.I2C0, freq=400000, scl=28, sda=29)

f = open('/sd/circle.png.bin','rb')
data = f.read()
f.close()

def draw_data(img, x, y, data):
    width = data[0] | (data[1]<<8)
    height = data[2] | (data[3]<<8)
    ring_buffer = [0] * 8
    ring_buffer_pointer = 0
    p = 4
    pos = 0
    while p<len(data):
        d = data[p] & 0x3F
        s = data[p] >> 6
        if s == 0:
            pos = pos + d + 1
            p = p + 1
        elif s == 1:
            rp = d >> 3
            rl = d & 7
            for i in range(rl + 1):
                xx = x + (pos % width)
                yy = y + (pos // width)
                col = ring_buffer[(rp + i)%len(ring_buffer)]
                img.set_pixel(xx,yy,col)
                pos = pos + 1
            p = p + 1
        elif s == 2:
            c = data[p+2] | (data[p+1] << 8)
            for i in range(d + 1):
                xx = x + (pos % width)
                yy = y + (pos // width)
                img.set_pixel(xx,yy,c)
                pos = pos + 1
            p = p + 3
        else:
            p = p + 1
            for i in range(d+1):
                tmp = data[p+1] | (data[p] << 8)
                xx = x + (pos % width)
                yy = y + (pos // width)
                img.set_pixel(xx,yy,tmp)
                ring_buffer[ring_buffer_pointer] = tmp
                ring_buffer_pointer = (ring_buffer_pointer + 1)%len(ring_buffer)
                pos = pos + 1
                p = p + 2

err_counter = 0
while 1:
    try:
        sensor.reset() #Reset sensor may failed, let's try some times
        break
    except:
        err_counter = err_counter + 1
        if err_counter == 20:
            lcd.draw_string(lcd.width()//2-100,lcd.height()//2-4, "Error: Sensor Init Failed", lcd.WHITE, lcd.RED)
        time.sleep(0.1)
        continue

sensor.set_pixformat(sensor.RGB565)
sensor.set_framesize(sensor.QVGA) #QVGA=320x240
sensor.set_windowing((224, 224))
sensor.run(1)

try:
    while(True):
        img = sensor.snapshot()
        draw_data(img, 0, 50, data)
        lcd.display(img)
except KeyboardInterrupt:
    sys.exit()
