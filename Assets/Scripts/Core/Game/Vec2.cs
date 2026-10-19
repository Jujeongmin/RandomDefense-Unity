namespace DefenseBattle.Core
{
    /// <summary>전장 논리 좌표 (720 x 720, 원점 좌상단, y 아래로 증가)</summary>
    public struct Vec2
    {
        public double X;
        public double Y;

        public Vec2(double x, double y)
        {
            X = x;
            Y = y;
        }

        public override string ToString() => "(" + X + ", " + Y + ")";
    }
}
