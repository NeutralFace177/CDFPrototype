#version 430 core

layout (local_size_x = 1, local_size_y = 1) in;
layout(rgba32f, binding = 1) uniform image2D imgOutput;

struct Fields2D {
    float d;
    float u;
    float v;
    float E;
    float S;
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
    Fields2D[] fields;
};

layout (std430, binding = 3) buffer out_data {
    Fields2D[] outFields;
};

uint coordToIndex(int i, int j) {
    return i*gl_NumWorkGroups.y+j;
}

ivec2 coords = ivec2(gl_GlobalInvocationID.xy);
uint index = coordToIndex(coords.x, coords.y);
iDataGroup4 indices = iDataGroup4(coordToIndex(coords.x+1,coords.y),coordToIndex(coords.x-1,coords.y),coordToIndex(coords.x,coords.y+1),coordToIndex(coords.x,coords.y-1));
uint width = gl_NumWorkGroups.x;
uint height = gl_NumWorkGroups.y;

float dx = 0.25;
float dy = 0.25;

float BC(int valId, int i, int j, int iOffset, int jOffset) {
    uint newIndex = coordToIndex(int(clamp(i+iOffset,0,width-1)),int(clamp(j+jOffset,0,height-1)));
    if (i+iOffset < 0 || i+iOffset >= width || j+jOffset < 0 || j+jOffset >= height) { 
            switch (valId) {
                //d
                case 0:
                    return fields[newIndex].d;
                //u
                case 1:
                    return 0;
                //v
                case 2:
                    return 0;
                //e
                case 3:
                    return fields[newIndex].E - 0.5 * (fields[newIndex].u*fields[newIndex].u+fields[newIndex].v*fields[newIndex].v);
                //S 
                case 4:
                    return fields[newIndex].S;
            }
    } else {
        switch (valId) {
            case 0:
                return fields[newIndex].d;
            case 1:
                return fields[newIndex].u;
            case 2:
                return fields[newIndex].v;
            case 3:
                return fields[newIndex].E - 0.5 * (fields[newIndex].u*fields[newIndex].u+fields[newIndex].v*fields[newIndex].v);
            case 4:
                return fields[newIndex].S;
        }
    }
}

vec3 calcStressTensor(int i, int j) {
    uint newIndex = coordToIndex(i,j);
    iDataGroup4 newIndices = iDataGroup4(coordToIndex(i+1,j),coordToIndex(i-1,j),coordToIndex(i,j+1),coordToIndex(i,j-1));
    float uDx = 0;
    float uDy = 0;
    float vDx = 0;
    float vDy = 0;
    if (i<0 || i >= width || j < 0 || j >= height) {
        uDx = (BC(1,i,j,0,0) < 0) ? (BC(1,i,j,1,0) - BC(1,i,j,0,0)) / dx : (BC(1,i,j,0,0) - BC(1,i,j,-1,0)) / dx;
        uDy = (BC(2,i,j,0,0) < 0) ? (BC(1,i,j,0,1) - BC(1,i,j,0,0)) / dy : (BC(1,i,j,0,0) - BC(1,i,j,0,-1)) / dy;
        vDx = (BC(1,i,j,0,0) < 0) ? (BC(2,i,j,1,0) - BC(2,i,j,0,0)) / dx : (BC(2,i,j,0,0) - BC(2,i,j,-1,0)) / dx;
        vDy = (BC(2,i,j,0,0) < 0) ? (BC(2,i,j,0,1) - BC(2,i,j,0,0)) / dy : (BC(2,i,j,0,0) - BC(1,i,j,0,-1)) / dy; 
    } else if (i==0 || i == width || j == 0 || j == height) {
        uDx = (fields[newIndex].u < 0) ? (BC(1,i,j,1,0) - fields[newIndex].u) / dx : (fields[newIndex].u - BC(1,i,j,-1,0)) / dx;
        uDy = (fields[newIndex].v < 0) ? (BC(1,i,j,0,1) - fields[newIndex].u) / dy : (fields[newIndex].u - BC(1,i,j,0,-1)) / dy;
        vDx = (fields[newIndex].u < 0) ? (BC(2,i,j,1,0) - fields[newIndex].v) / dx : (fields[newIndex].v - BC(2,i,j,-1,0)) / dx;
        vDy = (fields[newIndex].v < 0) ? (BC(2,i,j,0,1) - fields[newIndex].v) / dy : (fields[newIndex].v - BC(1,i,j,0,-1)) / dy; 
    } else {
        uDx = (fields[newIndex].u < 0) ? (fields[newIndices.right].u - fields[newIndex].u) / dx : (fields[newIndex].u - fields[newIndices.left].u) / dx;
        uDy = (fields[newIndex].v < 0) ? (fields[newIndices.up].u - fields[newIndex].u) / dy : (fields[newIndex].u - fields[newIndices.down].u) / dy;
        vDx = (fields[newIndex].u < 0) ? (fields[newIndices.right].v - fields[newIndex].v) / dx : (fields[newIndex].v - fields[newIndices.left].v) / dx;
        vDy = (fields[newIndex].v < 0) ? (fields[newIndices.up].v - fields[newIndex].v) / dy : (fields[newIndex].v - fields[newIndices.down].v) / dy;
    }
    float divU = uDx + vDy;
    float visc = 0.0000186;
    float visc2 = (2.0/3.0) * visc;
    //Txx Txy Tyy
    return vec3(visc2 * divU+2.0*visc*uDx, visc*(uDy+vDx), visc2*divU + 2.0*visc*vDy);
}

float calcPressure(int i, int j) {
    if (i < 0 || i >= width || j < 0 || j >= height) {
        return BC(0,i,j,0,0) * 0.286 * ((BC(3,i,j,0,0)-0.5*(BC(1,i,j,0,0)*BC(1,i,j,0,0)+BC(2,i,j,0,0)*BC(2,i,j,0,0)))/0.718);
    } else {
        Fields2D values = fields[coordToIndex(i,j)];
        return values.d * 0.286 * ((values.E-0.5*(values.u*values.u+values.v*values.v))/0.718);
    }
}

vec2 calcHeatFlux(int i, int j) {
    float TDx;
    float TDy;
    uint newIndex = coordToIndex(i,j);
    iDataGroup4 newIndices = iDataGroup4(coordToIndex(i+1,j),coordToIndex(i-1,j),coordToIndex(i,j+1),coordToIndex(i,j-1));
    float tmp; //temp temp (like temperary temperature)
    if (i < 0 || i >= width || j < 0 || j >= height) {
        tmp = (BC(3,i,j,0,0)-0.5*(BC(1,i,j,0,0)*BC(1,i,j,0,0)+BC(2,i,j,0,0)*BC(2,i,j,0,0)))/0.718;
        TDx = (BC(1,i,j,0,0) < 0) ? (((BC(3,i,j,1,0)-0.5*(BC(1,i,j,1,0)*BC(1,i,j,1,0)+BC(2,i,j,1,0)*BC(2,i,j,1,0)))/0.718)-tmp) / dx : (tmp - ((BC(3,i,j,-1,0)-0.5*(BC(1,i,j,-1,0)*BC(1,i,j,-1,0)+BC(2,i,j,-1,0)*BC(2,i,j,-1,0)))/0.718)) / dx;
        TDy = (BC(2,i,j,0,0) < 0) ? (((BC(3,i,j,0,1)-0.5*(BC(1,i,j,0,1)*BC(1,i,j,0,1)+BC(2,i,j,0,1)*BC(2,i,j,0,1)))/0.718)-tmp) / dy : (tmp - ((BC(3,i,j,0,-1)-0.5*(BC(1,i,j,0,-1)*BC(1,i,j,0,-1)+BC(2,i,j,0,-1)*BC(2,i,j,0,-1)))/0.718)) / dy;
    } else if (i==0 || i == width || j == 0 || j == height) {
        tmp = (fields[newIndex].E-0.5*(fields[newIndex].u*fields[newIndex].u+fields[newIndex].v*fields[newIndex].v))/0.718;
        TDx = (fields[newIndex].u < 0) ? (((BC(3,i,j,1,0)-0.5*(BC(1,i,j,1,0)*BC(1,i,j,1,0)+BC(2,i,j,1,0)*BC(2,i,j,1,0)))/0.718)-tmp) / dx : (tmp - ((BC(3,i,j,-1,0)-0.5*(BC(1,i,j,-1,0)*BC(1,i,j,-1,0)+BC(2,i,j,-1,0)*BC(2,i,j,-1,0)))/0.718)) / dx;
        TDy = (fields[newIndex].v < 0) ? (((BC(3,i,j,0,1)-0.5*(BC(1,i,j,0,1)*BC(1,i,j,0,1)+BC(2,i,j,0,1)*BC(2,i,j,0,1)))/0.718)-tmp) / dy : (tmp - ((BC(3,i,j,0,-1)-0.5*(BC(1,i,j,0,-1)*BC(1,i,j,0,-1)+BC(2,i,j,0,-1)*BC(2,i,j,0,-1)))/0.718)) / dy;
    } else {
        tmp = (fields[newIndex].E-0.5*(fields[newIndex].u*fields[newIndex].u+fields[newIndex].v*fields[newIndex].v))/0.718;
        TDx = (fields[newIndex].u < 0) ? (((fields[newIndices.right].E-0.5*(fields[newIndices.right].u*fields[newIndices.right].u+fields[newIndices.right].v*fields[newIndices.right].v))/0.718) - tmp) / dx : (tmp - ((fields[newIndices.left].E-0.5*(fields       [newIndices.left].u*fields[newIndices.left].u+fields[newIndices.left].v*fields[newIndices.left].v))/0.718)) / dx;
        TDy = (fields[newIndex].v < 0) ? (((fields[newIndices.up].E-0.5*(fields[newIndices.up].u*fields[newIndices.up].u+fields[newIndices.up].v*fields[newIndices.up].v))/0.718) - tmp) / dy : (tmp - ((fields[newIndices.up].E-0.5*(fields[newIndices.up].u*fields[newIndices.up].u+fields[newIndices.up].v*fields[newIndices.up].v))/0.718)) / dy;
    }
    return vec2(-0.02662 * TDx, -0.02662 * TDy);
}

vec2 calcSGradient(int i, int j ) {
    float SDx;
    float SDy;
    uint newIndex = coordToIndex(i,j);
    iDataGroup4 newIndices = iDataGroup4(coordToIndex(i+1,j),coordToIndex(i-1,j),coordToIndex(i,j+1),coordToIndex(i,j-1));
    if (i < 0 || i >= width || j < 0 || j >= height) {
        SDx = (BC(1,i,j,0,0) < 0) ? (BC(4,i,j,1,0) - BC(4,i,j,0,0)) / dx : (BC(4,i,j,0,0)-BC(4,i,j,-1,0)) / dx;
        SDy = (BC(2,i,j,0,0) < 0) ? (BC(4,i,j,0,1) - BC(4,i,j,0,0)) / dy : (BC(4,i,j,0,0)-BC(4,i,j,0,-1)) / dy;
    } else if (i == 0 || i == width || j == 0 || j == height) {
        SDx = (fields[newIndex].u < 0) ? (BC(4,i,j,1,0) - fields[newIndex].S) / dx : (fields[newIndex].S-BC(4,i,j,-1,0)) / dx;
        SDy = (fields[newIndex].u < 0) ? (BC(4,i,j,0,1) - fields[newIndex].S) / dy : (fields[newIndex].S-BC(4,i,j,0,-1)) / dy;
    } else {
        SDx = (fields[newIndex].u < 0) ? (fields[newIndices.right].S - fields[newIndex].S) / dx : (fields[newIndex].S-fields[newIndices.left].S) / dx;
        SDy = (fields[newIndex].u < 0) ? (fields[newIndices.up].S - fields[newIndex].S) / dy : (fields[newIndex].S-fields[newIndices.down].S) / dy;
    }
    return vec2(SDx, SDy);
}

float CD(int valId, int dim, bool forwards) {
    int i = coords.x;
    int j = coords.y;
    if (dim == 0) {
        switch (valId) {
            case 0:
                return (fields[index].d+BC(0,i,j,forwards?1:-1,0))/2.0;
            case 1:
                return (fields[index].u+BC(1,i,j,forwards?1:-1,0))/2.0;
            case 2:
                return (fields[index].v+BC(2,i,j,forwards?1:-1,0))/2.0;
            case 3:
                return (fields[index].E+BC(3,i,j,forwards?1:-1,0))/2.0;
            case 4:
                return (fields[index].S+BC(4,i,j,forwards?1:-1,0))/2.0;
        }
    } else {
        switch (valId) {
            case 0:
                return (fields[index].d+BC(0,i,j,0,forwards?1:-1))/2.0;
            case 1:
                return (fields[index].u+BC(1,i,j,0,forwards?1:-1))/2.0;
            case 2:
                return (fields[index].v+BC(2,i,j,0,forwards?1:-1))/2.0;
            case 3:
                return (fields[index].E+BC(3,i,j,0,forwards?1:-1))/2.0;
            case 4:
                return (fields[index].S+BC(4,i,j,0,forwards?1:-1))/2.0;
        }
    }
}

float FOU(int valId, int dim, bool forwards) {
    if (dim == 0) {
        switch (valId) {
            case 0:
                return fields[index].u >= 0 ? (forwards ? fields[index].d : BC(0,coords.x,coords.y,-1,0)) : (forwards ? BC(0,coords.x,coords.y,1,0) : fields[index].d);
            case 1:
                return fields[index].u >= 0 ? (forwards ? fields[index].u : BC(1,coords.x,coords.y,-1,0)) : (forwards ? BC(1,coords.x,coords.y,1,0) : fields[index].u);
            case 2:
                return fields[index].u >= 0 ? (forwards ? fields[index].v : BC(2,coords.x,coords.y,-1,0)) : (forwards ? BC(2,coords.x,coords.y,1,0) : fields[index].v);
            case 3:
                return fields[index].u >= 0 ? (forwards ? fields[index].E : BC(3,coords.x,coords.y,-1,0)) : (forwards ? BC(3,coords.x,coords.y,1,0) : fields[index].E);
            case 4:
                return fields[index].u >= 0 ? (forwards ? fields[index].S : BC(4,coords.x,coords.y,-1,0)) : (forwards ? BC(4,coords.x,coords.y,1,0) : fields[index].S);
        }
    } else {
        switch (valId) {
            case 0:
                return fields[index].v >= 0 ? (forwards ? fields[index].d : BC(0,coords.x,coords.y,0,-1)) : (forwards ? BC(0,coords.x,coords.y,0,1) : fields[index].d);
            case 1:
                return fields[index].v >= 0 ? (forwards ? fields[index].u : BC(1,coords.x,coords.y,0,-1)) : (forwards ? BC(1,coords.x,coords.y,0,1) : fields[index].u);
            case 2:
                return fields[index].v >= 0 ? (forwards ? fields[index].v : BC(2,coords.x,coords.y,0,-1)) : (forwards ? BC(2,coords.x,coords.y,0,1) : fields[index].v);
            case 3:
                return fields[index].v >= 0 ? (forwards ? fields[index].E : BC(3,coords.x,coords.y,0,-1)) : (forwards ? BC(3,coords.x,coords.y,0,1) : fields[index].E);
            case 4:
                return fields[index].v >= 0 ? (forwards ? fields[index].S : BC(4,coords.x,coords.y,0,-1)) : (forwards ? BC(4,coords.x,coords.y,0,1) : fields[index].S);
        }
    }
}


void main() {
    vec4 value = vec4(0.0, 0.0, 0.0, 1.0);
    DataGroupVec3 tensor = DataGroupVec3(calcStressTensor(coords.x,coords.y),calcStressTensor(coords.x+1,coords.y),calcStressTensor(coords.x-1,coords.y),calcStressTensor(coords.x,coords.y+1),calcStressTensor(coords.x,coords.y-1));
    DataGroupVec2 q = DataGroupVec2(calcHeatFlux(coords.x,coords.y),calcHeatFlux(coords.x+1,coords.y),calcHeatFlux(coords.x-1,coords.y),calcHeatFlux(coords.x,coords.y+1),calcHeatFlux(coords.x,coords.y-1));
    DataGroup p = DataGroup(calcPressure(coords.x,coords.y),calcPressure(coords.x+1,coords.y),calcPressure(coords.x-1,coords.y),calcPressure(coords.x,coords.y+1),calcPressure(coords.x,coords.y-1));
    DataGroupVec2 SGrad = DataGroupVec2(calcSGradient(coords.x,coords.y),calcSGradient(coords.x+1,coords.y),calcSGradient(coords.x-1,coords.y),calcSGradient(coords.x,coords.y+1),calcSGradient(coords.x,coords.y-1));


    float TxxXF = (tensor.center.x + tensor.right.x)/2.0;
    float TxxXB = (tensor.center.x + tensor.left.x)/2.0;

    float TxyXF = (tensor.center.y + tensor.right.y)/2.0;
    float TxyXB = (tensor.center.y + tensor.left.y)/2.0;
    float TxyYF = (tensor.center.y + tensor.up.y)/2.0;
    float TxyYB = (tensor.center.y + tensor.down.y)/2.0;

    float TyyYF = (tensor.center.z + tensor.up.z)/2.0;
    float TyyYB = (tensor.center.z + tensor.down.z)/2.0;

    float pXFC = (p.center + p.right)/2.0;
    float pXBC = (p.center + p.left)/2.0;
    float pYFC = (p.center + p.up)/2.0;
    float pYBC = (p.center + p.down)/2.0;

    float qxXF = (q.center.x + q.right.x)/2.0;
    float qxXB = (q.center.x + q.left.x)/2.0;
    float qyYF = (q.center.y + q.up.y)/2.0;
    float qyYB = (q.center.y + q.down.y)/2.0;

    float dXF = CD(0,0,true);
    float dXB = CD(0,0,false);
    float dYF = CD(0,1,true);
    float dYB = CD(0,1,false);

    float uXF = CD(1,0,true);
    float uXB = CD(1,0,false);
    float uYF = CD(1,1,true);
    float uYB = CD(1,1,false);

    float vXF = CD(2,0,true);
    float vXB = CD(2,0,false);
    float vYF = CD(2,1,true);
    float vYB = CD(2,1,false);

    float SXF = CD(4,0,true);
    float SXB = CD(4,0,false);
    float SYF = CD(4,1,true);
    float SYB = CD(4,1,false);

    float SDxXF = (SGrad.center.x + SGrad.right.x)/2.0;
    float SDxXB = (SGrad.center.x + SGrad.left.x)/2.0;
    float SDyYF = (SGrad.center.y + SGrad.up.y)/2.0;
    float SDyYB = (SGrad.center.y + SGrad.down.y)/2.0;

    float dt = 0.0006;
    float pressureToggle = 1.0;

    outFields[index].d = fields[index].d + dt * (-(dXF * uXF - dXB * uXB) / dx - (dYF * vYF - dYB * vYB) / dy);
    outFields[index].u = fields[index].u + (1.0 / fields[index].d) * dt * (-((dXF * uXF * uXF + pressureToggle * pXFC) - (dXB * uXB * uXB + pressureToggle * pXBC)) / dx -
                        (dYF * uYF * vYF - dYB * uYB * vYB) / dy + (TxxXF - TxxXB) / dx + (TxyYF - TxyYB) / dy);
    outFields[index].v = fields[index].v + (1.0 / fields[index].d) * dt * (-(dXF * uXF * vXF - dXB * uXB * vXB) / dx
                        - ((dYF * vYF * vYF + pressureToggle * pYFC) - (dYB * vYB * vYB + pressureToggle * pYBC)) / dy 
                        + (TxyXF - TxyXB) / dx + (TyyYF - TyyYB) / dy);
    outFields[index].E = fields[index].E + (1.0 / fields[index].d) * dt * (-(uXF * (dXF * CD(3,0,true) + pressureToggle * pXFC) - uXB * (dXB * CD(3,0,false) + pressureToggle * pXBC)) / dx
                     - (vYF * (dYF * CD(3,1,true) + pressureToggle * pYFC) - vYB * (dYB * CD(3,1,false) + pressureToggle * pYBC)) / dy
                     +((uXF * TxxXF + vXF * TxyXF - qxXF) - (uXB * TxxXB + vXB * TxyXB - qxXB)) / dx
                     + ((uYF * TxyYF + vYF * TyyYF - qyYF) - (uYB * TxyYB + vYB * TyyYB - qyYB)) / dy);;
    outFields[index].S = fields[index].S + (1.0 / fields[index].d) * dt * (-(dXF * uXF * SXF - dXB * uXB * SXB) / dx - (dYF * vYF * SYF - dYB * vYB * SYB) / dy + 0.05*(SDxXF - SDxXB) / dx + 0.05*(SDyYF - SDyYB) / dy);


    value.x = sqrt(fields[index].u*fields[index].u+fields[index].v*fields[index].v);
    value.y = fields[index].E / 50.0;
    value.z = fields[index].d/2.0;
    imageStore(imgOutput, coords, value);
}

