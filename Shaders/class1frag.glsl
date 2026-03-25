#version 400 core
out vec4 FragColor;
in vec2 vertexUV;

uniform sampler2D texture1;

float E = 2.7182818284590452353602874713527;
float PI = 3.14159265358979323846264338327950288;

float F(float x) {
    return float(floor(((255.0 * exp(-1.0*pow(E, -1000.0 * x))) * pow(abs(x), exp(-1.0*pow(E, -1000.0 * x))))));
}

float T(float s, float x, float y)
{
    return PI * (3.0*x+y)/3.0 + ((7.0+5.0*cos(pow(s,2.0)))/35.0)*pow(y-(x/3.0),2.0)*cos((6.0+3.0*cos(4.0*pow(s,2.0)))*(3.0*y-x)/3.0 + 2.0*cos(4.0*y+2.0*x+5.0*pow(s,2.0))+3.0*pow(s,2.0));
}

float Q(float x, float y)
{
    return ((3.0*x+y+(3.0/2.0)) / (2.0*pow(x+(y/3.0)+0.5,2.0)+12.0*pow(y-(x/3.0),2.0)+(1.0/100000.0))) + ((x+(y/3.0)-0.5) / (pow(x+(y/3.0)-0.5,2.0)+6.0*pow(y-(x/3.0),2.0)+(1.0/100000.0)));
}

float P(float x, float y)
{
    return ((3.0 * y - x) / 3.0)*((3.0/(2.0*pow(x+(y/3.0)+0.5,2.0)+12.0*pow(y-(x/3.0),2.0)+(1.0/100000.0)) )+(1.0/(pow(x+(y/3.0)-0.5,2.0)+6.0*pow(y-(x/3.0),2.0)+(1.0/100000.0))));
}

float U(float x, float y)
{
    return cos(2.0*log(pow(Q(x,y),2.0)+pow(P(x,y),2.0)))*Q(x,y)+sin(2.0*log(pow(Q(x,y),2.0)+pow(P(x,y),2.0)))*P(x,y);
}

float V(float x, float y)
{
    return atan(Q(x,y)/abs(P(x,y)));
}

float B(float x, float y)
{
    float result = 0.0;
    for (int i = 1; i <= 50; i++)
    {
        result += exp(-1.0*pow(E,-100.0*(pow(sin(T(float(i),x,y)),6.0)-(199.0/200.0)))-exp(pow(cos(200.0*T(float(i),x,y)+pow(float(i),2.0)),6.0)/-20.0)-exp(1000.0*(abs(x+(y/3.0))-(7.0/10.0))));
    }
    return result;
}

float A(float s, float x, float y)
{
    return exp(-1.0 * exp(0.5*sqrt(pow(x+(y/3.0)-0.5,2.0)+6.0*pow(y-(x/3.0),2.0))*sqrt(pow(x+(y/3.0)+0.5,2.0)+6.0*pow(y-(x/3.0),2.0))*abs(U(x,y)+pow(cos(7.0*pow(s,2.0)),4.0))-(7.0/20.0)))+exp(-1.0*exp(10.0*(pow(x+(y/3.0)+0.5,2.0)+6.0*pow(y-(x/3.0),2.0)-(3.0/100.0))))+ exp(-1.0 * exp(10.0 * (pow(x + (y / 3.0) - 0.5, 2.0) + 6.0 * pow(y - (x / 3.0), 2.0) - (3.0 / 100.0))));
}

float L(float u, float s, float x, float y)
{
    return cos(u*pow(s,2.0))*U(x,y)+(3.0/2.0)*sin(u*pow(s,2.0))*V(x,y);
}

float K(float v, float x, float y)
{
    float result = 0.0;
    for (int i = 1; i<=50; i++)
    {
        result += pow(19.0 / 20.0, float(i)) * ((3.0/2.0)+0.5*pow(v-1.0,2.0)+((4.0-4.0*v)/10.0)*cos(pow(float(i),2.0))) * exp(-1.0*exp((-3.0/2.0)*(cos(pow(5.0/4.0,float(i))*L(1.0,float(i),x,y)+cos(3.0*pow(5.0/4.0,float(i))*L(3.0,float(i),x,y))+2.0*cos(32.0*pow(float(i),2.0)))*cos(pow(5.0/4.0,float(i))*(sin(pow(float(i),2.0))*U(x,y)-(3.0/2.0)*cos(pow(float(i),2.0))*V(x,y))+cos(3.0*pow(5.0/4.0,float(i))*L(5.0,float(i),x,y))+2.0*cos(12.0*pow(float(i),2.0)))-(3.0/2.0)+A(float(i),x,y))));
    }
    return result;
}

float H(float v, float x,  float y) {
    return exp(-1.0*pow(E, -50.0 * (y - (x / 3.0) - (7.0 / 50.0)))) * (B(x, y) / 5.0) + ((20.0 * K(v, x, y)) / 37.0)*(1.0-exp(-1.0*pow(E,200.0*(pow(x+(y/3.0)+(1.0/2.0),2.0)+pow(y-(x/3.0)+(3.0/100.0),2.0)-(3.0/100.0))+exp(-200.0*(y-(x/3.0)+(2.0/5.0)*sqrt(abs((3.0/100.0)-pow(x+(y/3.0)+(1.0/2.0),2.0))))))))*(1.0-exp(-1.0*pow(E,200.0*(pow(x+(y/3.0)-(1.0/2.0),2.0)+pow(y-(x/3.0)+(3.0/100.0),2.0)-(1.0/50.0))+exp(-200.0*(y-(x/3.0)+(2.0/5.0)*sqrt(abs((1.0/50.0)-pow(x+(y/3.0)-0.5,2.0))))))));
}

void main()
{
    float x = gl_FragCoord.x+0.5;
    float y = gl_FragCoord.y+0.5;
    FragColor = vec4(F(H(0.0,(x-1000.0)/500.0,(601.0-y)/500.0))/255.0,F(H(1.0,(x-1000.0)/500.0,(601.0-y)/500.0))/255.0,F(H(2.0,(x-1000.0)/500.0,(601.0-y)/500.0)/255.0), 1.0);
}












