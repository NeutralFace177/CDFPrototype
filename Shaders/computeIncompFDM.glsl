#version 430 core

layout (local_size_x = 1, local_size_y = 1) in;
layout(rgba32f, binding = 1) uniform image2D imgOutput;

//https://gist.github.com/983/e170a24ae8eba2cd174f
vec3 rgb2hsv(vec3 c)
{
    vec4 K = vec4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    vec4 p = mix(vec4(c.bg, K.wz), vec4(c.gb, K.xy), step(c.b, c.g));
    vec4 q = mix(vec4(p.xyw, c.r), vec4(c.r, p.yzx), step(p.x, c.r));

    float d = q.x - min(q.w, q.y);
    float e = 1.0e-10;
    return vec3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
}
vec3 hsv2rgb(vec3 c)
{
    vec4 K = vec4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    vec3 p = abs(fract(c.xxx + K.xyz) * 6.0 - K.www);
    return c.z * mix(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
}

struct OutData {
    float u;
    float v;
    float a_x;
    float a_y;
    float a_c;
};

struct Fields2D {
    float u;
    float v;
};

struct MeshData {
    int obj;
    int reindex;
};

struct coordIndexPair {
    int i;
    int j;
    int index;
};

struct DebugThing {
    int f2d;
};

struct DataGroup {
    float center;
    float right;
    float left;
    float up;
    float down;
};

struct DataGroupVec2 {
    vec2 center;
    vec2 right;
    vec2 left;
    vec2 up;
    vec2 down;
};

struct DataGroupVec3 {
    vec3 center;
    vec3 right;
    vec3 left;
    vec3 up;
    vec3 down;
};

struct iDataGroup4 {
    uint right;
    uint left;
    uint up;
    uint down;
};
layout (std430, binding = 2) buffer shader_data {
    float dx;
    float dy;
    float dt;
    float d;
    Fields2D[] fields;
};


layout (std430, binding = 3) buffer out_data {
    OutData[] outData;
};

layout (std430, binding = 4) buffer mesh_data {
    MeshData[] mesh;
};

//layout (std430, binding = 5) buffer out_debug {
 //   DebugThing[] debug;
//};

//layout (std430, binding = 6) buffer prevData {
//    Fields2D[] prevFields;
//};

uint coordToIndex(int i, int j) {
    return i*gl_NumWorkGroups.y+j;
}
uint width = gl_NumWorkGroups.x;
uint height = gl_NumWorkGroups.y;
ivec2 coords = ivec2(gl_GlobalInvocationID.xy);
int i = coords.x;
int j = coords.y;
uint index = coordToIndex(coords.x, coords.y);
iDataGroup4 indices = iDataGroup4(coordToIndex(coords.x+1,coords.y),coordToIndex(coords.x-1,coords.y),coordToIndex(coords.x,coords.y+1),coordToIndex(coords.x,coords.y-1));

float BC(int valId, int i, int j, int iOffset, int jOffset) {
    uint newIndex = coordToIndex(int(clamp(i+iOffset,0,int(width-1))),int(clamp(j+jOffset,0,int(height-1))));
    bool objectFlag = false;
    if (mesh[newIndex].obj == 1) {
        newIndex = coordToIndex(i,j);
        objectFlag = true;
    }
    if (objectFlag) {
        switch (valId) {
            case 0:
                return d;
            case 1:
                return 0;
            case 2:
                return 0;
        }
    } else if (i+iOffset < 0) { 
        switch (valId) {
            //d
            case 0:
                return d;
            //u
            case 1:
                return 50.0;
            //v
            case 2:
                return 0;  
        }
    } else if (i+iOffset >= width) {
        switch (valId) {
            //d
            case 0:
                return d;
            //u
            case 1:
                return fields[newIndex].u;
            //v
            case 2:
                return fields[newIndex].v;
        }
    } else if (j+jOffset < 0) {
        switch (valId) {
            //d
            case 0:
                return d;
            //u
            case 1:
                return fields[newIndex].u;
            //v
            case 2:
                return fields[newIndex].v;
        }
    } else if (j+jOffset >= height) {
        switch (valId) {
            //d
            case 0:
                return d;
            //u
            case 1:
                return fields[newIndex].u;
            //v
            case 2:
                return fields[newIndex].v;
        }
    } else {
        switch (valId) {
            case 0:
                return d;
            case 1:
                return fields[newIndex].u;
            case 2:
                return fields[newIndex].v;
        }
    }
}

void main() {
    if (mesh[index].obj == 1) {
        outData[index].u = 0;
        outData[index].v = 0;
    } else {
        outData[index].u = fields[index].u + dt*(-max(fields[index].u,0) * (fields[index].u-BC(1,i,j,-1,0))/dx - min(fields[index].u,0) * (BC(1,i,j,1,0)-fields[index].u)/dx 
            - max(fields[index].v,0) * (fields[index].u-BC(1,i,j,0,-1))/dy - min(fields[index].v,0) * (BC(1,i,j,0,1)-fields[index].u)/dy 
            + 0.0000151 * ((BC(1,i,j,1,0)-2*fields[index].u+BC(1,i,j,-1,0))/(dx*dx) + (BC(1,i,j,0,1)-2*fields[index].u+BC(1,i,j,0,-1))/(dy*dy)));
        outData[index].v = fields[index].v + dt * (-max(fields[index].u,0) * (fields[index].v-BC(2,i,j,-1,0))/dx - min(fields[index].u,0) * (BC(2,i,j,1,0)-fields[index].v)/dx 
            - max(fields[index].v,0) * (fields[index].v-BC(2,i,j,0,-1))/dy - min(fields[index].v,0) * (BC(2,i,j,0,1)-fields[index].v)/dy 
            + 0.0000151 * ((BC(2,i,j,1,0)-2*fields[index].v+BC(2,i,j,-1,0))/(dx*dx) + (BC(2,i,j,0,1)-2*fields[index].v+BC(2,i,j,0,-1))/(dy*dy)));
        
        outData[index].a_x = dt/(d*dx*dx);
        outData[index].a_y = dt/(d*dy*dy);
        outData[index].a_c = -2*(outData[index].a_x + outData[index].a_y);
    }
} 

