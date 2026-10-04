#pragma once

/// \brief Library version
#define LIBRARY_VERSION 1

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
    void draw(int x, int y) const;

    /// \brief Scale the widget
    float scale(float factor = 1.f);

    /// \brief Resize to a square
    void resize(int size);

    /// \brief Resize to a rectangle
    void resize(int width, int height);

    /// \brief Get the value
    int value() const;

    /// \brief Count all widgets
    static int count();

    /// \brief Drawing mode
    enum class Mode
    {
        A,     ///< Mode A
        B = 2, ///< Mode B
        C      ///< Mode C
    };

    /// \brief Identifier type
    using Id = int;

    int field; ///< Public field

protected:
    /// \brief Update the widget
    virtual void update();

private:
    void secret();
};

/// \brief Class that gets removed
class Removed
{
public:
    /// \brief Member of the removed class
    void member();
};

/// \brief Base class, named so it's read after the derived class
class Shape
{
public:
    /// \brief Function of the base class
    void draw();
};

/// \brief Derived class
class Circle : public Shape
{
public:
    /// \brief Function of the derived class
    float radius() const;
};

/// \brief Free function
void freeFunction(int a);
} // namespace lib
