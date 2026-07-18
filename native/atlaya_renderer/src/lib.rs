use std::ffi::c_char;
use std::slice;

static VERSION_BYTES: &[u8] = concat!(env!("CARGO_PKG_VERSION"), "\0").as_bytes();

const LIGHT_X: f64 = -0.4082482904638631;
const LIGHT_Y: f64 = -0.4082482904638631;
const LIGHT_Z: f64 = 0.8164965809277261;

const HALF_X: f64 = -0.2141864952980661;
const HALF_Y: f64 = -0.2141864952980661;
const HALF_Z: f64 = 0.9530206138714225;

#[repr(C)]
pub struct NativeLeaf {
    pub left: f64,
    pub top: f64,
    pub right: f64,
    pub bottom: f64,
    pub ax: f64,
    pub bx: f64,
    pub ay: f64,
    pub by: f64,
    pub blue: u8,
    pub green: u8,
    pub red: u8,
    pub alpha: u8,
}

#[repr(C)]
pub struct NativeBorder {
    pub left: f64,
    pub top: f64,
    pub right: f64,
    pub bottom: f64,
    pub depth: i32,
}

#[no_mangle]
pub extern "C" fn atlaya_renderer_version() -> *const c_char {
    VERSION_BYTES.as_ptr().cast()
}

/// # Safety
/// The caller must provide valid pointers for the leaf, border, and pixel buffers
/// when the corresponding element counts or lengths are greater than zero.
#[no_mangle]
pub unsafe extern "C" fn atlaya_render_scene(
    leaves: *const NativeLeaf,
    leaf_count: i32,
    borders: *const NativeBorder,
    border_count: i32,
    width: i32,
    height: i32,
    ambient_light: f64,
    show_borders: i32,
    pixels: *mut u8,
    pixels_len: usize,
) -> u8 {
    if pixels.is_null() || width <= 0 || height <= 0 {
        return 0;
    }

    let width = width as usize;
    let height = height as usize;
    let required_len = match width.checked_mul(height).and_then(|v| v.checked_mul(4)) {
        Some(value) => value,
        None => return 0,
    };

    if pixels_len < required_len {
        return 0;
    }

    let pixel_buffer = slice::from_raw_parts_mut(pixels, required_len);
    fill_background(pixel_buffer);

    render_scene_internal(
        leaves,
        leaf_count,
        borders,
        border_count,
        width,
        height,
        ambient_light,
        show_borders,
        pixel_buffer,
    )
}

/// # Safety
/// The caller must provide valid pointers for the leaf, border, and pixel buffers
/// when the corresponding element counts or lengths are greater than zero.
#[no_mangle]
pub unsafe extern "C" fn atlaya_render_overlay(
    leaves: *const NativeLeaf,
    leaf_count: i32,
    borders: *const NativeBorder,
    border_count: i32,
    width: i32,
    height: i32,
    ambient_light: f64,
    show_borders: i32,
    pixels: *mut u8,
    pixels_len: usize,
) -> u8 {
    if pixels.is_null() || width <= 0 || height <= 0 {
        return 0;
    }

    let width = width as usize;
    let height = height as usize;
    let required_len = match width.checked_mul(height).and_then(|v| v.checked_mul(4)) {
        Some(value) => value,
        None => return 0,
    };

    if pixels_len < required_len {
        return 0;
    }

    let pixel_buffer = slice::from_raw_parts_mut(pixels, required_len);

    render_scene_internal(
        leaves,
        leaf_count,
        borders,
        border_count,
        width,
        height,
        ambient_light,
        show_borders,
        pixel_buffer,
    )
}

unsafe fn render_scene_internal(
    leaves: *const NativeLeaf,
    leaf_count: i32,
    borders: *const NativeBorder,
    border_count: i32,
    width: usize,
    height: usize,
    ambient_light: f64,
    show_borders: i32,
    pixel_buffer: &mut [u8],
) -> u8 {
    if show_borders != 0 && !borders.is_null() && border_count > 0 {
        let borders = slice::from_raw_parts(borders, border_count as usize);
        for border in borders {
            draw_border(pixel_buffer, width, height, border);
        }
    }

    if !leaves.is_null() && leaf_count > 0 {
        let leaves = slice::from_raw_parts(leaves, leaf_count as usize);
        let specular_light = 1.0 - ambient_light;
        for leaf in leaves {
            render_leaf(
                pixel_buffer,
                width,
                height,
                leaf,
                ambient_light,
                specular_light,
            );
        }
    }

    1
}

fn fill_background(pixels: &mut [u8]) {
    for chunk in pixels.chunks_exact_mut(4) {
        chunk[0] = 0x2E;
        chunk[1] = 0x1A;
        chunk[2] = 0x1A;
        chunk[3] = 0xFF;
    }
}

fn draw_border(pixels: &mut [u8], width: usize, height: usize, border: &NativeBorder) {
    let thickness = if border.depth <= 1 { 2 } else { 1 };

    let x1 = border.left.max(0.0) as i32;
    let y1 = border.top.max(0.0) as i32;
    let x2 = border.right.min(width as f64).floor() as i32 - 1;
    let y2 = border.bottom.min(height as f64).floor() as i32 - 1;

    if x2 <= x1 || y2 <= y1 {
        return;
    }

    for offset in 0..thickness {
        let tx1 = x1 + offset;
        let ty1 = y1 + offset;
        let tx2 = x2 - offset;
        let ty2 = y2 - offset;

        if tx2 <= tx1 || ty2 <= ty1 {
            break;
        }

        for x in tx1..=tx2 {
            write_pixel(pixels, width, height, x, ty1, 0x0F, 0x0F, 0x1A);
            write_pixel(pixels, width, height, x, ty2, 0x0F, 0x0F, 0x1A);
        }

        for y in (ty1 + 1)..ty2 {
            write_pixel(pixels, width, height, tx1, y, 0x0F, 0x0F, 0x1A);
            write_pixel(pixels, width, height, tx2, y, 0x0F, 0x0F, 0x1A);
        }
    }
}

fn render_leaf(
    pixels: &mut [u8],
    width: usize,
    height: usize,
    leaf: &NativeLeaf,
    ambient_light: f64,
    specular_light: f64,
) {
    let x1 = leaf.left.ceil().max(0.0) as i32;
    let y1 = leaf.top.ceil().max(0.0) as i32;
    let x2 = leaf.right.floor().min(width as f64) as i32 - 1;
    let y2 = leaf.bottom.floor().min(height as f64) as i32 - 1;

    if x2 < x1 || y2 < y1 {
        return;
    }

    let red = leaf.red as f64 / 255.0;
    let green = leaf.green as f64 / 255.0;
    let blue = leaf.blue as f64 / 255.0;

    for y in y1..=y2 {
        let dhy = 2.0 * leaf.ay * y as f64 + leaf.by;
        for x in x1..=x2 {
            let dhx = 2.0 * leaf.ax * x as f64 + leaf.bx;

            let mut nx = -dhx;
            let mut ny = -dhy;
            let inv_len = 1.0 / (nx * nx + ny * ny + 1.0).sqrt();
            nx *= inv_len;
            ny *= inv_len;
            let nz = inv_len;

            let diffuse = (nx * LIGHT_X + ny * LIGHT_Y + nz * LIGHT_Z).max(0.0);
            let diffuse_component = ambient_light + specular_light * diffuse;

            let nh = (nx * HALF_X + ny * HALF_Y + nz * HALF_Z).max(0.0);
            let nh2 = nh * nh;
            let nh4 = nh2 * nh2;
            let spec = nh4 * nh4 * nh4 * nh4 * nh4;

            let spec_strength = 0.70 * spec;
            let spec_red = spec_strength * (red * 0.55 + 0.45);
            let spec_green = spec_strength * (green * 0.55 + 0.45);
            let spec_blue = spec_strength * (blue * 0.55 + 0.45);

            write_pixel(
                pixels,
                width,
                height,
                x,
                y,
                clamp_to_byte(blue * diffuse_component + spec_blue),
                clamp_to_byte(green * diffuse_component + spec_green),
                clamp_to_byte(red * diffuse_component + spec_red),
            );
        }
    }
}

fn write_pixel(
    pixels: &mut [u8],
    width: usize,
    height: usize,
    x: i32,
    y: i32,
    blue: u8,
    green: u8,
    red: u8,
) {
    if x < 0 || y < 0 || x as usize >= width || y as usize >= height {
        return;
    }

    let index = (y as usize * width + x as usize) * 4;
    pixels[index] = blue;
    pixels[index + 1] = green;
    pixels[index + 2] = red;
    pixels[index + 3] = 0xFF;
}

fn clamp_to_byte(value: f64) -> u8 {
    if value <= 0.0 {
        0
    } else if value >= 1.0 {
        255
    } else {
        (value * 255.0 + 0.5) as u8
    }
}
