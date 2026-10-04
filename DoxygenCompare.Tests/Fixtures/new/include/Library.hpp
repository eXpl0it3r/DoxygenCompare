#pragma once

/// \brief Library version
#define LIBRARY_VERSION 2

/// \brief Example library
namespace lib
{
/// \brief Example class
class Widget
{
public:
    /// \brief Construct the widget
    Widget();

    /// \brief Draw the widget
    /// \deprecated Use something else
    void draw(int x, int y) const;

    /// \brief Scale the widget
    float scale(float factor = 2.f);

    /// \brief Resize to a square
    void resize(int size);

    /// \brief Resize to a rectangle
    void resize(int width, int height);

    /// \brief Resize with a long
    void resize(long size);

    /// \brief Get the value
    long value() const;

    /// \brief Count widgets
    static int count(bool all);

    /// \brief Drawing mode
    enum class Mode
    {
        A,     ///< Mode A
        B = 3, ///< Mode B
        D      ///< Mode D
    };

    /// \brief Identifier type
    using Id = long;

    int    field;      ///< Public field
    double addedField; ///< Added field

protected:
    /// \brief Update the widget
    void update();

private:
    void secret(int value);
};

/// \brief Class that got added
class Added
{
public:
    /// \brief Member of the added class
    void member();
};

/// \brief Base class, named so it's read after the derived class
class Shape
{
public:
    /// \brief Function of the base class
    void draw();

    /// \brief Function added to the base class
    void setMiterLimit(float miterLimit);
};

/// \brief Derived class
class Circle : public Shape
{
public:
    /// \brief Function of the derived class
    float radius() const;
};

/// \brief Free function
void freeFunction(int a) noexcept;
} // namespace lib
